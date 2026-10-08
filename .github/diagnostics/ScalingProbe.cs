using System;
using System.Drawing;
using System.Windows.Forms;

// Temporary diagnostic: measures how AutoScaleMode.Font transforms designer coordinates
// on a real Windows session, so runtime-created controls can use the same factor.
// Every measurement is printed on a single PROBE| line so the workflow can turn it into
// a GitHub annotation (sandbox browsers cannot open the raw job logs).
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
                showNote = "show-failed:" + ex.GetType().Name;
            }

            Console.WriteLine("PROBE|" + name
                + "|AutoScaleDims=" + form.AutoScaleDimensions
                + "|CurrentDims=" + form.CurrentAutoScaleDimensions
                + "|Font=" + form.Font.Name + " " + form.Font.SizeInPoints.ToString("0.##") + "pt h=" + form.Font.Height
                + "|Label=" + label.Bounds
                + "|Factors=X" + ((double)label.Left / 322.0).ToString("0.###")
                + "/W" + ((double)label.Width / 196.0).ToString("0.###")
                + "|FontHeight/20=" + ((double)form.Font.Height / 20.0).ToString("0.###")
                + "|" + showNote);
            form.Hide();
        }
        catch (Exception ex)
        {
            Console.WriteLine("PROBE|" + name + "|FAILED|" + ex.GetType().Name + ": " + ex.Message);
        }
        finally
        {
            form.Dispose();
        }
    }

    [STAThread]
    private static void Main()
    {
        string dpi = "?";
        using (Form dpiForm = new Form())
        {
            using (Graphics g = dpiForm.CreateGraphics())
            {
                dpi = g.DpiX + "x" + g.DpiY;
            }
        }

        Console.WriteLine("PROBE|env|OS=" + Environment.OSVersion
            + "|64bit=" + Environment.Is64BitProcess
            + "|GraphicsDpi=" + dpi
            + "|DefaultFont=" + Control.DefaultFont.Name + " "
            + Control.DefaultFont.SizeInPoints.ToString("0.##") + "pt h=" + Control.DefaultFont.Height);

        Probe("A-baseline9x20-ambient-font", new SizeF(9f, 20f), 0f);
        Probe("B-baseline9x20-ms8.25", new SizeF(9f, 20f), 8.25f);
        Probe("C-baseline6x13-ambient-font", new SizeF(6f, 13f), 0f);
        Probe("D-baseline6x13-ms8.25", new SizeF(6f, 13f), 8.25f);
        Probe("E-baseline-empty-ambient-font", new SizeF(0f, 0f), 0f);
    }
}
