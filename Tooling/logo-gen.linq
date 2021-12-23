<Query Kind="Statements">
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Drawing2D</Namespace>
</Query>

var size = 24; //24

var m1 = 1; //Margin 1: start
var s1 = size - m1; //Margin 1: width

var m2 = 2; //Margin 2: start
var s2 = size - m2; //Margin 2: width

var brushSize = size / 8; //3
var gapSize = size / 12; //2

var center = size / 2;

using (var bitmap = new Bitmap(size, size))
using (var graphics = Graphics.FromImage(bitmap))
{
    //Creates a alpha mask used for logo:
    var bg = Color.Black;
    var lines = Color.White;
    graphics.FillRectangle(new SolidBrush(bg), 0, 0, size, size);
    
    graphics.SmoothingMode = SmoothingMode.AntiAlias;
    
    var penLines = new Pen(lines, brushSize);

    //X
    graphics.DrawLine(penLines, m2, m2, s2, s2); // -> \
    graphics.DrawLine(penLines, s2, m2, m2, s2); // -> /

    var penBg = new Pen(bg, gapSize);
    
    //Cross in the middle:
    graphics.DrawLine(penBg, center, 00, center, size); // -> Verticaly
    graphics.DrawLine(penBg, 00, center, size, center); // -> Horizontally

    //Smooth edges:
    graphics.DrawLine(penBg, m1, m1, s1, m1); // -> Top
    graphics.DrawLine(penBg, m1, m1, m1, s1); // -> Left
    graphics.DrawLine(penBg, m1, s1, s1, s1); // -> Bottom
    graphics.DrawLine(penBg, s1, m1, s1, s1); // -> Right

    bitmap.Dump();
}