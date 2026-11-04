using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Portraiture;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace MacPortraitFix;

public sealed class Settings
{
    public bool Enabled { get; set; } = true;
    // Optional explicit rows for unusual user-supplied images, keyed by NPC internal name.
    public Dictionary<string, int> ExpressionCounts { get; set; } = new() { ["Lewis:1696x2528"] = 5 };
    public Dictionary<string, int> RowsByNpc { get; set; } = new() { ["Harvey"] = 6, ["Lewis"] = 3 };
}

public sealed class ModEntry : Mod
{
    internal static ModEntry Instance = null!;
    internal Settings Config = new();
    private static readonly FieldInfo? PortraitureConfigField = AccessTools.Field(typeof(PortraitureMod), "config");
    private static readonly PropertyInfo? AboveBoxProperty = PortraitureConfigField?.FieldType.GetProperty("ShowPortraitsAboveBox");
    private readonly HashSet<string> reported = new();
    private List<double>? samples;
    private long lastFrame;
    private string benchmarkContext = "";
    private bool? benchmarkMenu;
    public override void Entry(IModHelper helper)
    {
        Instance = this;
        Config = helper.ReadConfig<Settings>();
        helper.WriteConfig(Config);
        helper.Events.GameLoop.GameLaunched += (_, _) =>
        {
            try
            {
                new Harmony(ModManifest.UniqueID).Patch(
                    AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.drawPortrait)),
                    transpiler: new HarmonyMethod(typeof(ModEntry), nameof(Transpile)));
                Monitor.Log("高清对话头像补丁已加载；使用原始高清纹理，保留现有 Portraiture 选包与季节规则。", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"高清头像补丁未能加载，保留原有显示：{ex}", LogLevel.Error);
            }
        };
        helper.ConsoleCommands.Add("hdportrait_status", "显示当前对话的头像纹理尺寸。", (_, _) =>
        {
            if (Game1.activeClickableMenu is not DialogueBox box || box.characterDialogue?.speaker is not NPC npc)
            { Monitor.Log("请先打开 NPC 对话。", LogLevel.Info); return; }
            var tex = box.characterDialogue.overridePortrait ?? npc.Portrait;
            if (tex is ScaledTexture2D scaled)
                Monitor.Log($"{npc.Name}: 原图 {scaled.STexture.Width}×{scaled.STexture.Height}，表情编号 {box.characterDialogue.getPortraitIndex()}，补丁启用={Config.Enabled}", LogLevel.Info);
            else Monitor.Log($"{npc.Name}: 当前纹理 {tex.Width}×{tex.Height}，未获得 Portraiture 高清纹理；请按 P 选取头像包。", LogLevel.Info);
        });
        helper.ConsoleCommands.Add("mac_fps", "采样接下来 600 个实际绘制帧；保持前台和同一场景。", (_, _) =>
        {
            samples = new List<double>(600); lastFrame = 0;
            benchmarkContext = Game1.currentLocation?.Name ?? "title";
            benchmarkMenu = Game1.activeClickableMenu != null;
            Monitor.Log("开始采样 600 帧；请保持当前场景和菜单状态。", LogLevel.Info);
        });
        helper.Events.Display.Rendered += (_, _) =>
        {
            if (samples is null) return;
            if ((Game1.currentLocation?.Name ?? "title") != benchmarkContext || (Game1.activeClickableMenu != null) != benchmarkMenu)
            { samples = null; Monitor.Log("场景或菜单状态改变，已取消本次帧率采样。", LogLevel.Info); return; }
            if (!GameRunner.instance.IsActive) { lastFrame = 0; return; }
            long now = Stopwatch.GetTimestamp();
            if (lastFrame != 0) samples.Add((now - lastFrame) * 1000.0 / Stopwatch.Frequency);
            lastFrame = now;
            if (samples.Count < 600) return;
            double average = samples.Average(); var sorted = samples.OrderBy(x => x).ToArray();
            Monitor.Log($"帧率采样 [{benchmarkContext}]：平均 {1000 / average:F1} FPS；帧时间平均 {average:F2} ms，P95 {sorted[569]:F2} ms，P99 {sorted[593]:F2} ms；超过 25 ms：{samples.Count(x => x > 25)}/600。", LogLevel.Info);
            samples = null;
        };
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { reported.Clear(); samples = null; };
    }

    private static readonly Type[] DrawArgs = { typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float) };
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var source = AccessTools.Method(typeof(SpriteBatch), nameof(SpriteBatch.Draw), DrawArgs);
        var target = AccessTools.Method(typeof(ModEntry), nameof(DrawPortrait));
        var result = new List<CodeInstruction>(); int matches = 0;
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(source))
            {
                // Preserve branch labels on the new first instruction.
                var loadBox = new CodeInstruction(OpCodes.Ldarg_0);
                loadBox.labels.AddRange(instruction.labels); instruction.labels.Clear();
                loadBox.blocks.AddRange(instruction.blocks); instruction.blocks.Clear();
                result.Add(loadBox);
                result.Add(new CodeInstruction(OpCodes.Call, target)); matches++;
            }
            else result.Add(instruction);
        }
        if (matches == 0) throw new InvalidOperationException("Unsupported DialogueBox drawing method; no matching draw call.");
        return result;
    }

    public static void DrawPortrait(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source,
        Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth, DialogueBox box)
    {
        // Cursor/frame/name drawing is passed through untouched. Animated and explicit HDP crops retain their own renderer.
        if (Instance.Config.Enabled && AboveBoxProperty?.GetValue(PortraitureConfigField?.GetValue(null)) is not true && source is Rectangle { Width: 64, Height: 64 }
            && texture is ScaledTexture2D hd && texture is not AnimatedTexture2D
            && hd.ForcedSourceRectangle is null && hd.STexture is Texture2D native
            && box.characterDialogue?.speaker is NPC npc && box.characterDialogue.overridePortrait is null)
        {
            int columns = native.Width == 256 && native.Height == 256 ? 1 : 2;
            int inferredRows = Math.Max(1, (int)Math.Round(native.Height / (native.Width / (double)columns)));
            int rows = inferredRows;
            if (Instance.Config.RowsByNpc.TryGetValue(npc.Name, out int configured) && configured > 0
                && Math.Abs(native.Height / (native.Width / (double)columns) - configured) < 0.15)
                rows = configured;
            int index = box.characterDialogue.getPortraitIndex();
            int count = columns * rows;
            if (Instance.Config.ExpressionCounts.TryGetValue($"{npc.Name}:{native.Width}x{native.Height}", out int available) && available > 0)
                count = Math.Min(count, available);
            if (index < 0 || index >= count) index = 0;
            Rectangle crop = PortraitGrid.Cell(native.Width, native.Height, columns, rows, index);
            if (crop.Width > 64 && crop.Height > 64)
            {
                // Integer boundaries cover the entire original grid, including its odd final pixel.
                batch.Draw(native, position, crop, color, rotation,
                    new Vector2(origin.X * crop.Width / 64f, origin.Y * crop.Height / 64f),
                    new Vector2(64f * scale / crop.Width, 64f * scale / crop.Height), effects, depth);
                if (Instance.reported.Add($"{npc.Name}:{native.Width}x{native.Height}"))
                    Instance.Monitor.Log($"高清直绘 {npc.Name}: {native.Width}×{native.Height}, {columns}列×{rows}行, 表情格 {crop.Width}×{crop.Height}", LogLevel.Debug);
                return;
            }
        }
        batch.Draw(texture, position, source, color, rotation, origin, scale, effects, depth);
    }
}
