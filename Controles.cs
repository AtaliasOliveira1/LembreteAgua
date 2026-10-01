using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LembreteAgua;

// ============ TEMA: cores e fontes num lugar só ============
public static class Tema
{
    public static readonly Color Fundo = Color.FromArgb(18, 20, 26);
    public static readonly Color Card = Color.FromArgb(30, 33, 42);
    public static readonly Color Borda = Color.FromArgb(52, 56, 70);
    public static readonly Color Texto = Color.FromArgb(235, 238, 245);
    public static readonly Color TextoSuave = Color.FromArgb(140, 148, 165);
    public static readonly Color Azul = Color.FromArgb(56, 189, 248);
    public static readonly Color Verde = Color.FromArgb(52, 211, 153);
    public static readonly Color Vermelho = Color.FromArgb(244, 63, 94);

    // Fontes criadas uma vez só (criar fonte a cada desenho gasta memória)
    public static readonly Font Normal = new("Segoe UI", 9f);
    public static readonly Font Pequeno = new("Segoe UI", 8.5f, FontStyle.Bold);
    public static readonly Font Negrito = new("Segoe UI", 9.5f, FontStyle.Bold);
    public static readonly Font Campo = new("Segoe UI", 10.5f);
    public static readonly Font Medio = new("Segoe UI", 13f, FontStyle.Bold);
    public static readonly Font Titulo = new("Segoe UI", 15f, FontStyle.Bold);
    public static readonly Font Grande = new("Segoe UI", 28f, FontStyle.Bold);

    // Retângulo com cantos arredondados
    public static GraphicsPath Arredondado(Rectangle r, int raio)
    {
        var p = new GraphicsPath();
        int d = raio * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int valor, int tamanho);

    // Deixa a janela sem borda, escura e com cantos redondos (Windows 11)
    public static void Moldar(Form f)
    {
        f.FormBorderStyle = FormBorderStyle.None;
        f.BackColor = Fundo;
        f.Paint += (_, e) =>
        {
            using var pen = new Pen(Borda);
            e.Graphics.DrawRectangle(pen, 0, 0, f.Width - 1, f.Height - 1);
        };
        f.HandleCreated += (_, _) =>
        {
            try { int v = 2; DwmSetWindowAttribute(f.Handle, 33, ref v, 4); } catch { }
        };
    }
}

// ============ ÍCONES FLAT desenhados por código ============
public static class Icones
{
    private static Bitmap Novo(int s, out Graphics g)
    {
        var b = new Bitmap(s, s);
        g = Graphics.FromImage(b);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        return b;
    }

    public static Bitmap Gota(int s, Color c)
    {
        var b = Novo(s, out var g);
        using (g)
        using (var br = new SolidBrush(c))
        {
            float cx = s / 2f, r = s * .33f, cy = s * .64f;
            g.FillEllipse(br, cx - r, cy - r, 2 * r, 2 * r);
            g.FillPolygon(br, new[] { new PointF(cx, s * .05f),
                new PointF(cx - r * .94f, cy - r * .35f), new PointF(cx + r * .94f, cy - r * .35f) });
        }
        return b;
    }

    public static Bitmap Recipiente(int ml, int s, Color c)
    {
        var b = Novo(s, out var g);
        using (g)
        using (var br = new SolidBrush(c))
        {
            if (ml <= 300) // copo
                g.FillPolygon(br, new[] { new PointF(s * .2f, s * .12f), new PointF(s * .8f, s * .12f),
                    new PointF(s * .7f, s * .9f), new PointF(s * .3f, s * .9f) });
            else           // garrafa
            {
                g.FillRectangle(br, s * .28f, s * .28f, s * .44f, s * .64f);
                g.FillRectangle(br, s * .40f, s * .12f, s * .20f, s * .18f);
                g.FillRectangle(br, s * .36f, s * .04f, s * .28f, s * .08f);
            }
        }
        return b;
    }

    public static Bitmap Engrenagem(int s, Color c)
    {
        var b = Novo(s, out var g);
        using (g)
        using (var anel = new Pen(c, s * .12f))
        using (var dente = new Pen(c, s * .16f))
        {
            float m = s / 2f;
            g.DrawEllipse(anel, m - s * .2f, m - s * .2f, s * .4f, s * .4f);
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4;
                g.DrawLine(dente,
                    (float)(m + Math.Cos(a) * s * .3), (float)(m + Math.Sin(a) * s * .3),
                    (float)(m + Math.Cos(a) * s * .43), (float)(m + Math.Sin(a) * s * .43));
            }
        }
        return b;
    }

    public static Bitmap Fechar(int s, Color c)
    {
        var b = Novo(s, out var g);
        using (g)
        using (var p = new Pen(c, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawLine(p, s * .2f, s * .2f, s * .8f, s * .8f);
            g.DrawLine(p, s * .8f, s * .2f, s * .2f, s * .8f);
        }
        return b;
    }

    public static Bitmap Minimizar(int s, Color c)
    {
        var b = Novo(s, out var g);
        using (g)
        using (var p = new Pen(c, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawLine(p, s * .2f, s * .5f, s * .8f, s * .5f);
        return b;
    }

    // Ícone da janela e da bandeja
    public static Icon CriarIcone() => Icon.FromHandle(Gota(32, Tema.Azul).GetHicon());
}

// ============ BOTÃO FLAT ============
public class BotaoFlat : Control
{
    public Color Cor { get; set; } = Tema.Azul;
    public bool Contorno { get; set; }   // só borda, sem preenchimento
    public bool Discreto { get; set; }   // sem fundo até passar o mouse (botões da barra de título)

    private Bitmap? icone;
    public Bitmap? Icone { get => icone; set { icone = value; Invalidate(); } }

    private bool hover, pressionado;

    public BotaoFlat()
    {
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        Font = Tema.Negrito;
        Size = new Size(120, 40);
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = pressionado = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { pressionado = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pressionado = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Tema.Fundo);

        Color cor = Enabled ? Cor : Tema.Borda;
        var r = Discreto ? new Rectangle(4, 4, Width - 9, Height - 9) : new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Tema.Arredondado(r, Discreto ? 8 : 12);
        Color corTexto;

        if (Discreto)
        {
            if (hover && Enabled) { using var b = new SolidBrush(cor); g.FillPath(b, path); }
            corTexto = Tema.Texto;
        }
        else if (Contorno)
        {
            if (hover && Enabled) { using var b = new SolidBrush(Color.FromArgb(32, cor)); g.FillPath(b, path); }
            using var p = new Pen(cor, 1.5f);
            g.DrawPath(p, path);
            corTexto = cor;
        }
        else
        {
            Color f = !Enabled ? cor : pressionado ? ControlPaint.Dark(cor, .1f)
                    : hover ? ControlPaint.Light(cor, .25f) : cor;
            using var b = new SolidBrush(f);
            g.FillPath(b, path);
            corTexto = Tema.Fundo;
        }

        // Ícone + texto centralizados juntos
        var flags = TextFormatFlags.NoPadding;
        Size t = string.IsNullOrEmpty(Text) ? Size.Empty
               : TextRenderer.MeasureText(g, Text, Font, new Size(int.MaxValue, int.MaxValue), flags);
        int iw = icone?.Width ?? 0;
        int gap = (icone != null && t.Width > 0) ? 8 : 0;
        int x = (Width - (iw + gap + t.Width)) / 2;

        if (icone != null) g.DrawImage(icone, x, (Height - icone.Height) / 2);
        if (t.Width > 0)
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x + iw + gap, 0, t.Width + 4, Height),
                corTexto, flags | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }
}

// ============ ANEL DE PROGRESSO (animado) ============
public class AnelProgresso : Control
{
    private double alvo, atual;
    private string principal = "0%", secundario = "";
    private readonly System.Windows.Forms.Timer anim = new() { Interval = 16 };

    public AnelProgresso()
    {
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        anim.Tick += (_, _) =>
        {
            atual += (alvo - atual) * 0.15;               // aproxima aos poucos = animação suave
            if (Math.Abs(alvo - atual) < 0.001) { atual = alvo; anim.Stop(); }
            Invalidate();
        };
    }

    public void Definir(double pct, string textoPrincipal, string textoSecundario)
    {
        alvo = pct; principal = textoPrincipal; secundario = textoSecundario;
        anim.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) anim.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Tema.Fundo);

        int esp = 16;
        var r = new Rectangle(esp, esp, Width - 2 * esp - 1, Height - 2 * esp - 1);
        using (var fundo = new Pen(Tema.Card, esp - 2)) g.DrawEllipse(fundo, r);
        if (atual > 0.001)
        {
            using var arco = new Pen(alvo >= 1 ? Tema.Verde : Tema.Azul, esp - 2)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(arco, r, -90, (float)(360 * Math.Min(atual, 1)));
        }

        var fl = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
        TextRenderer.DrawText(g, principal, Tema.Grande, new Rectangle(0, Height / 2 - 34, Width, 50), Tema.Texto, fl);
        TextRenderer.DrawText(g, secundario, Tema.Normal, new Rectangle(0, Height / 2 + 16, Width, 22), Tema.TextoSuave, fl);
    }
}

// ============ CAMPO DE TEXTO ARREDONDADO ============
public class Campo : Panel
{
    public readonly TextBox Caixa = new()
    {
        BorderStyle = BorderStyle.None, BackColor = Tema.Card, ForeColor = Tema.Texto, Font = Tema.Campo
    };

    public Campo(bool somenteNumeros)
    {
        DoubleBuffered = true;
        BackColor = Tema.Fundo;
        Height = 38;
        Controls.Add(Caixa);
        Caixa.MaxLength = somenteNumeros ? 4 : 30;
        if (somenteNumeros)
            Caixa.KeyPress += (_, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
        Caixa.Enter += (_, _) => Invalidate();
        Caixa.Leave += (_, _) => Invalidate();
        Click += (_, _) => Caixa.Focus();
    }

    public string Texto { get => Caixa.Text; set => Caixa.Text = value; }

    // Lê o número digitado e força ele a ficar entre min e max
    public int Inteiro(int min, int max) =>
        Math.Clamp(int.TryParse(Caixa.Text, out int v) ? v : min, min, max);

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Caixa.Location = new Point(12, (Height - Caixa.Height) / 2);
        Caixa.Width = Math.Max(10, Width - 24);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Tema.Arredondado(new Rectangle(0, 0, Width - 1, Height - 1), 10);
        using var b = new SolidBrush(Tema.Card);
        g.FillPath(b, path);
        using var p = new Pen(Caixa.Focused ? Tema.Azul : Tema.Borda, 1.5f);
        g.DrawPath(p, path);
    }
}

// ============ INTERRUPTOR (liga/desliga) ============
public class Alternador : Control
{
    private bool marcado;
    public bool Checked { get => marcado; set { marcado = value; Invalidate(); } }

    public Alternador()
    {
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
        Size = new Size(44, 24);
        Cursor = Cursors.Hand;
    }

    protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Tema.Fundo);
        using (var path = Tema.Arredondado(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2))
        using (var b = new SolidBrush(marcado ? Tema.Azul : Tema.Borda))
            g.FillPath(b, path);
        int d = Height - 8;
        using var bola = new SolidBrush(Tema.Texto);
        g.FillEllipse(bola, marcado ? Width - d - 4 : 4, 4, d, d);
    }
}