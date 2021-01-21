<Query Kind="Statements">
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Drawing2D</Namespace>
</Query>

using (var bitmap = new Bitmap(24, 24))
using (var graphics = Graphics.FromImage(bitmap))
{
    //Creates a alpha mask used for logo:
    var bg = Color.Black;
    var lines = Color.White;
    graphics.FillRectangle(new SolidBrush(bg), 0, 0, 24, 24);
    
    graphics.SmoothingMode = SmoothingMode.AntiAlias;
    
    var penLines = new Pen(lines, 3);

    //X
    graphics.DrawLine(penLines, 02, 02, 22, 22); // -> \
    graphics.DrawLine(penLines, 22, 02, 02, 22); // -> /

    var penBg = new Pen(bg, 2);
    
    //Cross in the middle:
    graphics.DrawLine(penBg, 12, 00, 12, 24); // -> Verticaly
    graphics.DrawLine(penBg, 00, 12, 24, 12); // -> Horizontally

    //Smooth edges:
    graphics.DrawLine(penBg, 01, 01, 23, 01); // -> Top
    graphics.DrawLine(penBg, 01, 01, 01, 23); // -> Left
    graphics.DrawLine(penBg, 01, 23, 23, 23); // -> Bottom
    graphics.DrawLine(penBg, 23, 01, 23, 23); // -> Right

    bitmap.Dump();
}