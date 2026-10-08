using System;
using System.Drawing;
using System.Windows.Forms;

// Temporary diagnostic: measures how AutoScaleMode.Font transforms designer coordinates
// on a real Windows session, so runtime-created controls can use the same factor.
internal static class ScalingProbe
{
    private static void Probe(string name, SizeF baseline, float fontPoints)
    {
        Form form = new Form();
        try
        {
            form.AutoScaleDimensions = baseline;
            form.AutoScaleMode = AutoScaleMode.Font;
            if (fontPoints > 0f)
            {
                form.Font = new Font("MS Sans Serif", fontPoints);
            }

            Label label = new Label();
            label.Location = new Point(322, 124);
            label.Size = new Size(196, 32);
            form.Controls.Add(label);

            form.CreateControl();
            string showNote = "shown";
            try
            {
                form.Show();
            }
            catch (Exception ex)
            {
                showNote = "show failed: " + ex.Message;
            }

            Console.WriteLine("== " + name);
            Console.WriteLine("   AutoScaleDimensions   : " + form.AutoScaleDimensions);
            Console.WriteLine("   CurrentAutoScaleDims  : " + form.CurrentAutoScaleDimensions);
            Console.WriteLine("   Font                  : " + form.Font.Name + " " +
                              form.Font.SizeInPoints.ToString("0.##") + "pt height=" + form.Font.Height);
            Console.WriteLine("   label bounds          : " + label.Bounds);
            Console.WriteLine("   X factor              : " + ((double)label.Left / 322.0).ToString("0.####"));
            Console.WriteLine("   Y factor              : " + ((double)label.Top / 124.0).ToString("0.####"));
            Console.WriteLine("   Width factor          : " + ((double)label.Width / 196.0).ToString("0.####"));
            Console.WriteLine("   Height factor         : " + ((double)label.Height / 32.0).ToString("0.####"));
            Console.WriteLine("   Font.Height/20        : " + ((double)form.Font.Height / 20.0).ToString("0.####"));
            Console.WriteLine("   form state            : " + showNote);
            form.Hide();
        }
        finally
        {
            form.Dispose();
        }
    }

    [STAThread]
    private static void Main()
    {
        Console.WriteLine("OS            : " + Environment.OSVersion);
        Console.WriteLine("64-bit process: " + Environment.Is64BitProcess);

        using (Form dpiForm = new Form())
        {
            using (Graphics g = dpiForm.CreateGraphics())
            {
                Console.WriteLine("DpiX/DpiY     : " + g.DpiX + "/" + g.DpiY);
            }
        }

        Console.WriteLine("DefaultFont   : " + Control.DefaultFont.Name + " " +
                          Control.DefaultFont.SizeInPoints.ToString("0.##") + "pt height=" +
                          Control.DefaultFont.Height);

        Probe("A: baseline 9x20 (Form1), ambient font", new SizeF(9f, 20f), 0f);
        Probe("B: baseline 9x20, explicit MS Sans Serif 8.25pt", new SizeF(9f, 20f), 8.25f);
        Probe("C: baseline 6x13 (EditChainDialog style)", new SizeF(6f, 13f), 0f);
        Probe("D: baseline 6x13, explicit MS Sans Serif 8.25pt", new SizeF(6f, 13f), 8.25f);
    }
}
