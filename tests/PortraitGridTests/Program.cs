using MacPortraitFix;
foreach (var (w,h,c,r) in new[]{(1371,4096,2,6),(1365,4096,2,6),(1696,2528,2,3),(848,2560,2,6),(512,1536,2,6),(2048,2048,2,2),(256,256,1,1)}) {
 long area=0;
 for(int row=0;row<r;row++){
  int right=0;
  for(int col=0;col<c;col++){
   var cell=PortraitGrid.Cell(w,h,c,r,row*c+col);
   if(cell.X!=right||cell.Width<=0||cell.Height<=0||cell.Y+cell.Height>h)throw new Exception("Gap/overlap/out-of-bounds");
   right=cell.X+cell.Width;area+=(long)cell.Width*cell.Height;
  }
  if(right!=w)throw new Exception("Last column cropped");
 }
 if(area!=(long)w*h)throw new Exception("Pixel loss");
 var last=PortraitGrid.Cell(w,h,c,r,c*r-1);
 if(last.Y+last.Height!=h)throw new Exception("Final expression cropped");
 Console.WriteLine($"PASS {w}x{h}: {c*r} expressions, full pixel coverage");
}
try { PortraitGrid.Cell(512,512,2,2,4);throw new Exception("Invalid index accepted"); } catch(ArgumentOutOfRangeException) { Console.WriteLine("PASS invalid index rejected"); }
namespace Microsoft.Xna.Framework {
 public readonly record struct Rectangle(int X,int Y,int Width,int Height);
}
