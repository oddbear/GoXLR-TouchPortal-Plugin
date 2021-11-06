<Query Kind="Statements">
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Drawing2D</Namespace>
</Query>

var width = 100;
var height = 100;
var margin = 5;

using (var bitmap = new Bitmap(width, height))
using (var graphics = Graphics.FromImage(bitmap))
{
    //Creates a alpha mask used for logo:
    var bg = Color.Black;
	var lines = Color.Gray;

	//bitmap.MakeTransparent();
	graphics.SmoothingMode = SmoothingMode.AntiAlias;
    
    var penLines = new Pen(lines, 3);

	var penBg = new Pen(bg, 5);

	graphics.FillEllipse(new SolidBrush(lines), margin, margin, width - 2 * margin, height - 2 * margin);
	graphics.DrawEllipse(penBg, margin, margin, width - 2 * margin, height - 2 * margin);

	//Smooth edges:
	graphics.DrawLines(penBg, new[] {
		new Point(width / 4, height / 2),
		new Point(width / 2, height / 4 * 3),
		new Point(height / 4 * 3, height / 4),
	});

    bitmap.Dump();
}