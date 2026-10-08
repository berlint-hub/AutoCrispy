using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

// Layout regression check for the real AutoCrispy main form.
// Loads AutoCrispy.exe, opens Form1 with the Spandrel backend selected, then resizes the
// window and simulates DPI scaling. Reports overlapping sibling controls, controls that
// leave their container, and clipped text. Overlaps/clipping fail the build; text that is
// shortened with an ellipsis is reported as a warning.
internal static class LayoutCheck
{
    private const int OverlapTolerancePixels = 4;
    private static readonly List<string> Errors = new List<string>();
    private static readonly List<string> Warnings = new List<string>();

    private static readonly Size[] WindowSizes =
    {
        new Size(820, 560), new Size(900, 600), new Size(980, 640),
        new Size(1100, 700), new Size(1280, 800), new Size(1600, 900)
    };

    private static readonly float[] Scales = { 1.0f, 1.25f, 1.5f };

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("usage: LayoutCheck <AutoCrispy.exe>");
            return 2;
        }

        Application.EnableVisualStyles();
        Assembly app = Assembly.LoadFrom(Path.GetFullPath(args[0]));
        Type formType = app.GetType("AutoCrispy.Form1", true);

        foreach (float scale in Scales)
        {
            string scaleLabel = (scale * 100f).ToString("0") + "%";
            Form form = (Form)Activator.CreateInstance(formType);
            try
            {
                form.Show();
                Pump(3000);
                SelectSpandrel(form);
                Pump(500);
                if (scale != 1.0f)
                {
                    form.Scale(new SizeF(scale, scale));
                    // Mirror Form1_DpiChanged, which re-reads the responsive margins.
                    InvokePrivate(form, "ConfigureResponsiveLayout");
                    InvokePrivate(form, "ApplyResponsiveLayout");
                    Pump(300);
                }

                foreach (Size size in WindowSizes)
                {
                    form.Size = size;
                    Pump(250);
                    Visit(form, scaleLabel + " window " + size.Width + "x" + size.Height);
                }
            }
            catch (Exception ex)
            {
                Errors.Add("harness exception @" + scaleLabel + ": " + ex.GetBaseException().Message);
            }
            finally
            {
                form.Close();
                form.Dispose();
            }
        }

        foreach (string w in Warnings)
        {
            Console.WriteLine("::warning title=Layout text::" + Escape(w));
        }
        foreach (string e in Errors)
        {
            Console.WriteLine("::error title=Layout::" + Escape(e));
        }
        Console.WriteLine("LayoutCheck: " + Errors.Count + " error(s), " + Warnings.Count + " warning(s).");
        return Errors.Count == 0 ? 0 : 1;
    }

    private static string Escape(string s)
    {
        return s.Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A");
    }

    private static void Pump(int milliseconds)
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }

    private static void SelectSpandrel(Form form)
    {
        PropertyInfo prop = form.GetType().GetProperty("ExeComboBox",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        ComboBox combo = prop == null ? null : prop.GetValue(form, null) as ComboBox;
        if (combo == null)
        {
            Errors.Add("ExeComboBox not found");
            return;
        }
        if (!combo.Items.Contains("Spandrel")) combo.Items.Add("Spandrel");
        combo.SelectedItem = "Spandrel";
    }

    private static void InvokePrivate(object target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method != null && method.GetParameters().Length == 0) method.Invoke(target, null);
    }

    // Children of these controls are internal parts (spin buttons, edit boxes, dropdown
    // parts) or tab pages sharing one display area; they are not laid out by AutoCrispy.
    private static bool IsInternalContainer(Control c)
    {
        return c is NumericUpDown || c is ComboBox || c is TextBox || c is ListBox || c is TabControl;
    }

    private static void Visit(Control parent, string context)
    {
        if (IsInternalContainer(parent)) return;

        List<Control> visible = new List<Control>();
        foreach (Control child in parent.Controls)
        {
            if (!child.Visible || child.Width <= 0 || child.Height <= 0) continue;
            visible.Add(child);
        }

        Rectangle parentClient = parent is Form
            ? Rectangle.Empty
            : parent.RectangleToScreen(parent.ClientRectangle);

        for (int i = 0; i < visible.Count; i++)
        {
            Control a = visible[i];
            Rectangle ra = ScreenBounds(a);
            CheckText(a, context);

            if (!(parent is Form) && !Contains(parentClient, ra))
            {
                Errors.Add(Describe(a) + " leaves its container " + Describe(parent) + " (" + context + ")");
            }

            for (int j = i + 1; j < visible.Count; j++)
            {
                Rectangle rb = ScreenBounds(visible[j]);
                Rectangle inter = Rectangle.Intersect(ra, rb);
                if (inter.Width > OverlapTolerancePixels && inter.Height > OverlapTolerancePixels)
                {
                    Errors.Add(Describe(a) + " overlaps " + Describe(visible[j]) + " by " +
                               inter.Width + "x" + inter.Height + "px (" + context + ")");
                }
            }
        }

        foreach (Control child in visible)
        {
            Visit(child, context);
        }
    }

    private static Rectangle ScreenBounds(Control c)
    {
        Point origin = c.Parent.PointToScreen(c.Location);
        return new Rectangle(origin, c.Size);
    }

    private static bool Contains(Rectangle outer, Rectangle inner)
    {
        const int tol = 1;
        return inner.Left >= outer.Left - tol && inner.Top >= outer.Top - tol &&
               inner.Right <= outer.Right + tol && inner.Bottom <= outer.Bottom + tol;
    }

    private static void CheckText(Control c, string context)
    {
        if (c.Parent is Form || string.IsNullOrWhiteSpace(c.Text)) return;
        Label label = c as Label;
        Button button = c as Button;
        CheckBox check = c as CheckBox;
        if (label == null && button == null && check == null) return;
        if (label != null && label.AutoSize) return;

        int glyph = check != null ? 22 : 0;
        int padding = (label != null ? label.Padding.Horizontal : 0) + 6;
        Size measured = TextRenderer.MeasureText(c.Text, c.Font,
            new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        int needed = measured.Width + glyph + padding;
        if (needed > c.ClientSize.Width + 2)
        {
            string text = Describe(c) + " text \"" + Trim(c.Text) + "\" needs " + needed +
                          "px, has " + c.ClientSize.Width + "px (" + context + ")";
            bool ellipsis = label != null && label.AutoEllipsis;
            if (ellipsis) Warnings.Add("ellipsis " + text);
            else Errors.Add("clipped " + text);
        }
    }

    private static string Trim(string s)
    {
        string one = s.Replace("\r", " ").Replace("\n", " ");
        return one.Length > 60 ? one.Substring(0, 60) + "..." : one;
    }

    private static string Describe(Control c)
    {
        string name = string.IsNullOrEmpty(c.Name) ? c.GetType().Name : c.Name;
        return name + "[" + c.Location.X + "," + c.Location.Y + " " + c.Width + "x" + c.Height + "]";
    }
}
