using Microsoft.Xna.Framework;
namespace MacPortraitFix;
public static class PortraitGrid
{
    public static Rectangle Cell(int width, int height, int columns, int rows, int index)
    {
        if (width <= 0 || height <= 0 || columns <= 0 || rows <= 0 || index < 0 || index >= columns * rows)
            throw new ArgumentOutOfRangeException(nameof(index));
        int col = index % columns, row = index / columns;
        int x = col * width / columns, y = row * height / rows;
        return new Rectangle(x, y, (col + 1) * width / columns - x, (row + 1) * height / rows - y);
    }
}
