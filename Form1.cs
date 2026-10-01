using System.Collections.Generic;
using System;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LembreteAgua
{
    public partial class Form1 : Form
    {
        // Permite arrastar a janela (que não tem borda) pela barra de título
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        // ---- Telas ----
        private readonly Panel pnlTitulo = new();
        private readonly Panel pnlHome = new();
        private readonly Panel pnlConfig = new();
        private BotaoFlat btnConfig = null!;

        // ---- Tela principal ----
        private AnelProgresso anel = null!;
        private Label lblSaudacao = null!, lblStatus = null!, lblAviso = null!;
        private FlowLayoutPanel flowRapido = null!;
        private BotaoFlat btnIniciar = null!, btnDesfazer = null!;

        // ---- Tela de configurações ----
        private Campo txtNome = null!, txtPeso = null!, txtAltura = null!, txtIntervalo = null!;
        private readonly Campo[] txtSlots = new Campo[5];
        private BotaoFlat btnMais = null!, btnMenos = null!, btnSalvar = null!;
        private Alternador tglWindows = null!, tglAuto = null!;

        // ---- Timers e bandeja ----
        private readonly System.Windows.Forms.Timer timerAgua = new();
        private readonly System.Windows.Forms.Timer timerAviso = new() { Interval = 3500 };
        private readonly NotifyIcon trayIcon = new();
        private ToolStripMenuItem miAlternar = null!;

        // ---- Estado ----
        private double litrosRecomendados = 0;
        private double litrosConsumidos = 0;
        private int slotsAtivos = 1;
        private bool popupAberto = false;
        private bool sair = false;
        private DateTime proximoLembrete;
        private readonly Stack<int> historico = new(); // para o botão "Desfazer"

        public Form1()
        {
            Configuracoes.Carregar();
            litrosConsumidos = Configuracoes.ConsumoHoje;

            Text = "Lembrete de Água by@ataliasloami_";
            ClientSize = new Size(420, 600);
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            DoubleBuffered = true;
            Icon = Icones.CriarIcone();
            Tema.Moldar(this);

            MontarTitulo();
            MontarHome();
            MontarConfig();

            lblAviso = Rotulo("", 0, 598, 420, 28, Tema.Negrito, Tema.Verde, ContentAlignment.MiddleCenter);
            Controls.Add(pnlHome);
            Controls.Add(pnlConfig);
            Controls.Add(pnlTitulo);
            Controls.Add(lblAviso);

            timerAgua.Tick += TimerAgua_Tick;
            timerAviso.Tick += (_, _) => { timerAviso.Stop(); lblAviso.Text = ""; };

            ConfigurarBandeja();
            CarregarDadosNaTela();

            if (Configuracoes.AutoIniciarLembretes) IniciarTimer();

            // Primeira vez: abre direto nas configurações
            if (Configuracoes.EhPrimeiraVez())
            {
                AlternarTela();
                Aviso("Bem-vindo! Preencha seus dados e salve.");
            }
        }

        // =============== CONSTRUÇÃO DA INTERFACE ===============

        private static Label Rotulo(string texto, int x, int y, int w, int h, Font fonte, Color cor,
            ContentAlignment alinhamento = ContentAlignment.MiddleLeft) =>
            new() { Text = texto, Location = new Point(x, y), Size = new Size(w, h), Font = fonte,
                    ForeColor = cor, TextAlign = alinhamento, AutoSize = false };

        private void MontarTitulo()
        {
            pnlTitulo.Location = Point.Empty;
            pnlTitulo.Size = new Size(420, 44);
            pnlTitulo.BackColor = Tema.Fundo;

            var logo = new PictureBox { Image = Icones.Gota(24, Tema.Azul), Location = new Point(16, 10), Size = new Size(24, 24) };
            var titulo = Rotulo("Lembrete de Água 1.0.0 by@ataliasloami_", 46, 8, 200, 28, Tema.Negrito, Tema.Texto);

            btnConfig = new BotaoFlat { Discreto = true, Cor = Tema.Card, Icone = Icones.Engrenagem(20, Tema.Texto), Size = new Size(44, 44), Location = new Point(288, 0) };
            var btnMin = new BotaoFlat { Discreto = true, Cor = Tema.Card, Icone = Icones.Minimizar(16, Tema.Texto), Size = new Size(44, 44), Location = new Point(332, 0) };
            var btnFechar = new BotaoFlat { Discreto = true, Cor = Tema.Vermelho, Icone = Icones.Fechar(16, Tema.Texto), Size = new Size(44, 44), Location = new Point(376, 0) };

            btnConfig.Click += (_, _) => AlternarTela();
            btnMin.Click += (_, _) => Hide();      // vai para a bandeja
            btnFechar.Click += (_, _) => Hide();   // idem (para sair de verdade: menu da bandeja)

            foreach (Control c in new Control[] { pnlTitulo, logo, titulo })
                c.MouseDown += (_, e) =>
                {
                    if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); }
                };

            pnlTitulo.Controls.AddRange(new Control[] { logo, titulo, btnConfig, btnMin, btnFechar });
        }

        private void MontarHome()
        {
            pnlHome.Location = new Point(0, 44);
            pnlHome.Size = new Size(420, 596);
            pnlHome.BackColor = Tema.Fundo;

            anel = new AnelProgresso { Location = new Point(110, 8), Size = new Size(200, 200) };
            lblSaudacao = Rotulo("", 0, 214, 420, 30, Tema.Medio, Tema.Texto, ContentAlignment.MiddleCenter);
            lblStatus = Rotulo("", 0, 244, 420, 24, Tema.Normal, Tema.TextoSuave, ContentAlignment.MiddleCenter);

            var lblRapido = Rotulo("REGISTRAR AGORA", 20, 286, 380, 20, Tema.Pequeno, Tema.TextoSuave);
            flowRapido = new FlowLayoutPanel { Location = new Point(14, 310), Size = new Size(392, 104), BackColor = Tema.Fundo };

            btnIniciar = new BotaoFlat { Text = "INICIAR LEMBRETES", Location = new Point(20, 428), Size = new Size(380, 48) };
            btnIniciar.Click += (_, _) => AlternarLembretes();

            btnDesfazer = new BotaoFlat { Text = "Desfazer último registro", Contorno = true, Cor = Tema.TextoSuave, Location = new Point(20, 486), Size = new Size(380, 40), Font = Tema.Normal };
            btnDesfazer.Click += (_, _) => DesfazerUltimo();

            pnlHome.Controls.AddRange(new Control[] { anel, lblSaudacao, lblStatus, lblRapido, flowRapido, btnIniciar, btnDesfazer });
        }

        private void MontarConfig()
        {
            pnlConfig.Location = new Point(0, 44);
            pnlConfig.Size = new Size(420, 596);
            pnlConfig.BackColor = Tema.Fundo;
            pnlConfig.Visible = false;

            pnlConfig.Controls.Add(Rotulo("SEU NOME", 20, 8, 380, 20, Tema.Pequeno, Tema.TextoSuave));
            txtNome = new Campo(false) { Location = new Point(20, 30), Width = 380 };

            pnlConfig.Controls.Add(Rotulo("PESO (KG)", 20, 84, 180, 20, Tema.Pequeno, Tema.TextoSuave));
            txtPeso = new Campo(true) { Location = new Point(20, 106), Width = 180 };

            pnlConfig.Controls.Add(Rotulo("ALTURA (CM)", 220, 84, 180, 20, Tema.Pequeno, Tema.TextoSuave));
            txtAltura = new Campo(true) { Location = new Point(220, 106), Width = 180 };

            pnlConfig.Controls.Add(Rotulo("LEMBRAR A CADA (MINUTOS)", 20, 160, 380, 20, Tema.Pequeno, Tema.TextoSuave));
            txtIntervalo = new Campo(true) { Location = new Point(20, 182), Width = 380 };

            pnlConfig.Controls.Add(Rotulo("TAMANHO DOS COPOS (ML)", 20, 236, 380, 20, Tema.Pequeno, Tema.TextoSuave));
            for (int i = 0; i < 5; i++)
            {
                txtSlots[i] = new Campo(true) { Location = new Point(20 + i * 78, 258), Width = 66 };
                pnlConfig.Controls.Add(txtSlots[i]);
            }

            btnMais = new BotaoFlat { Text = "+ Adicionar copo", Contorno = true, Location = new Point(20, 308), Size = new Size(180, 38) };
            btnMenos = new BotaoFlat { Text = "− Remover copo", Contorno = true, Cor = Tema.TextoSuave, Location = new Point(220, 308), Size = new Size(180, 38) };
            btnMais.Click += (_, _) => { if (slotsAtivos < 5) { slotsAtivos++; AtualizarSlots(); } };
            btnMenos.Click += (_, _) => { if (slotsAtivos > 1) { slotsAtivos--; AtualizarSlots(); } };

            pnlConfig.Controls.Add(Rotulo("Iniciar junto com o Windows", 20, 366, 320, 24, Tema.Normal, Tema.Texto));
            tglWindows = new Alternador { Location = new Point(356, 366) };
            pnlConfig.Controls.Add(Rotulo("Iniciar lembretes ao abrir o app", 20, 404, 320, 24, Tema.Normal, Tema.Texto));
            tglAuto = new Alternador { Location = new Point(356, 404) };

            btnSalvar = new BotaoFlat { Text = "SALVAR", Cor = Tema.Verde, Location = new Point(20, 460), Size = new Size(380, 48) };
            btnSalvar.Click += (_, _) => SalvarConfiguracoes();

            pnlConfig.Controls.AddRange(new Control[] { txtNome, txtPeso, txtAltura, txtIntervalo, btnMais, btnMenos, tglWindows, tglAuto, btnSalvar });
        }

        private void ConfigurarBandeja()
        {
            var menu = new ContextMenuStrip { BackColor = Tema.Card, ForeColor = Tema.Texto };
            menu.Items.Add("Abrir", null, (_, _) => MostrarJanela());
            miAlternar = new ToolStripMenuItem("Iniciar lembretes", null, (_, _) => AlternarLembretes());
            menu.Items.Add(miAlternar);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Sair", null, (_, _) => { sair = true; Application.Exit(); });

            trayIcon.Icon = Icones.CriarIcone();
            trayIcon.Text = "Lembrete de Água 1.0.0 by@ataliasloami_";
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (_, _) => MostrarJanela();
        }

        // =============== LÓGICA ===============

        private void AlternarTela()
        {
            bool irParaConfig = !pnlConfig.Visible;
            pnlConfig.Visible = irParaConfig;
            pnlHome.Visible = !irParaConfig;
            // Na tela de configurações o botão vira uma "gota" (voltar ao início)
            btnConfig.Icone = irParaConfig ? Icones.Gota(20, Tema.Azul) : Icones.Engrenagem(20, Tema.Texto);
        }

        private void CarregarDadosNaTela()
        {
            txtNome.Texto = string.IsNullOrWhiteSpace(Configuracoes.Nome) ? "Turquia" : Configuracoes.Nome;
            txtPeso.Texto = ((int)Configuracoes.Peso).ToString();
            txtAltura.Texto = ((int)Configuracoes.Altura).ToString();
            txtIntervalo.Texto = Configuracoes.IntervaloMinutos.ToString();
            tglWindows.Checked = Configuracoes.IniciarComWindows;
            tglAuto.Checked = Configuracoes.AutoIniciarLembretes;
            for (int i = 0; i < 5; i++) txtSlots[i].Texto = Configuracoes.CoposMl[i].ToString();

            slotsAtivos = Configuracoes.QuantidadeSlots;
            AtualizarSlots();
            MontarBotoesRapidos();
            CalcularMeta();
        }

        private void AtualizarSlots()
        {
            for (int i = 0; i < 5; i++) txtSlots[i].Visible = i < slotsAtivos;
            btnMais.Enabled = slotsAtivos < 5;
            btnMenos.Enabled = slotsAtivos > 1;
        }

        private void MontarBotoesRapidos()
        {
            // Libera os botões antigos antes de criar novos
            foreach (var c in flowRapido.Controls.Cast<Control>().ToList()) c.Dispose();
            flowRapido.Controls.Clear();

            for (int i = 0; i < Configuracoes.QuantidadeSlots; i++)
            {
                int ml = Configuracoes.CoposMl[i];
                if (ml <= 0) continue;
                var b = new BotaoFlat
                {
                    Text = $"+{ml} ml", Contorno = true, Icone = Icones.Recipiente(ml, 20, Tema.Azul),
                    Size = new Size(118, 44), Margin = new Padding(3)
                };
                b.Click += (_, _) => Registrar(ml);
                flowRapido.Controls.Add(b);
            }
        }

        private void SalvarConfiguracoes()
        {
            // Inteiro(min, max) garante que o valor digitado fique numa faixa válida
            Configuracoes.Nome = string.IsNullOrWhiteSpace(txtNome.Texto) ? "Turquia" : txtNome.Texto.Trim();
            Configuracoes.Peso = txtPeso.Inteiro(20, 250);
            Configuracoes.Altura = txtAltura.Inteiro(100, 230);
            Configuracoes.IntervaloMinutos = txtIntervalo.Inteiro(1, 360);
            Configuracoes.IniciarComWindows = tglWindows.Checked;
            Configuracoes.AutoIniciarLembretes = tglAuto.Checked;
            Configuracoes.QuantidadeSlots = slotsAtivos;
            for (int i = 0; i < 5; i++) Configuracoes.CoposMl[i] = txtSlots[i].Inteiro(50, 3000);

            Configuracoes.Salvar();
            CarregarDadosNaTela();                 // mostra os valores já corrigidos
            if (timerAgua.Enabled) IniciarTimer(); // aplica o novo intervalo
            AlternarTela();
            Aviso("Configurações salvas!");
        }

        private void CalcularMeta()
        {
            double basePeso = Configuracoes.Peso * 35.0;
            double ajusteAltura = (Configuracoes.Altura / 100.0) * 100.0;
            litrosRecomendados = (basePeso + ajusteAltura) / 1000.0;
            AtualizarProgresso();
        }

        private void Registrar(int ml)
        {
            VerificarNovoDia();
            litrosConsumidos += ml / 1000.0;
            historico.Push(ml);
            Configuracoes.ConsumoHoje = litrosConsumidos;
            Configuracoes.SalvarConsumo();
            AtualizarProgresso();
        }

        private void DesfazerUltimo()
        {
            if (historico.Count == 0) { Aviso("Nada para desfazer."); return; }
            litrosConsumidos = Math.Max(0, litrosConsumidos - historico.Pop() / 1000.0);
            Configuracoes.ConsumoHoje = litrosConsumidos;
            Configuracoes.SalvarConsumo();
            AtualizarProgresso();
        }

        private void VerificarNovoDia()
        {
            if (!Configuracoes.NovoDiaSeNecessario()) return;
            litrosConsumidos = 0;
            historico.Clear();
            AtualizarProgresso();
        }

        private void AtualizarProgresso()
        {
            double pct = litrosRecomendados > 0 ? Math.Min(1, litrosConsumidos / litrosRecomendados) : 0;
            anel.Definir(pct, $"{pct * 100:F0}%", $"{litrosConsumidos:F2} / {litrosRecomendados:F2} L");
            AtualizarStatus();
        }

        private void AtualizarStatus()
        {
            lblSaudacao.Text = $"Olá, {Configuracoes.Nome}!";
            if (litrosRecomendados > 0 && litrosConsumidos >= litrosRecomendados)
            {
                lblStatus.Text = "Meta de hoje atingida! Parabéns!";
                lblStatus.ForeColor = Tema.Verde;
            }
            else if (timerAgua.Enabled)
            {
                lblStatus.Text = $"Próximo lembrete às {proximoLembrete:HH:mm}";
                lblStatus.ForeColor = Tema.Azul;
            }
            else
            {
                lblStatus.Text = "Lembretes pausados";
                lblStatus.ForeColor = Tema.TextoSuave;
            }
        }

        private void Aviso(string mensagem)
        {
            lblAviso.Text = mensagem;
            timerAviso.Stop();
            timerAviso.Start();
        }

        // =============== LEMBRETES ===============

        private void AlternarLembretes()
        {
            if (timerAgua.Enabled) PararTimer(); else IniciarTimer();
        }

        private void IniciarTimer()
        {
            int minutos = Configuracoes.IntervaloMinutos;
            timerAgua.Interval = minutos * 60 * 1000;
            timerAgua.Start();
            proximoLembrete = DateTime.Now.AddMinutes(minutos);

            btnIniciar.Text = "PARAR LEMBRETES";
            btnIniciar.Cor = Tema.Vermelho;
            miAlternar.Text = "Parar lembretes";
            AtualizarStatus();
        }

        private void PararTimer()
        {
            timerAgua.Stop();
            btnIniciar.Text = "INICIAR LEMBRETES";
            btnIniciar.Cor = Tema.Azul;
            miAlternar.Text = "Iniciar lembretes";
            AtualizarStatus();
        }

        private void TimerAgua_Tick(object? sender, EventArgs e)
        {
            VerificarNovoDia();
            proximoLembrete = DateTime.Now.AddMilliseconds(timerAgua.Interval);
            if (!popupAberto) ExibirPopup();
            AtualizarStatus();
        }

        private void ExibirPopup()
        {
            popupAberto = true;
            SystemSounds.Asterisk.Play();

            var slots = Enumerable.Range(0, Configuracoes.QuantidadeSlots)
                                  .Select(i => Configuracoes.CoposMl[i]).Where(m => m > 0).ToList();
            int linhas = (int)Math.Ceiling(slots.Count / 2.0);

            var popup = new Form
            {
                ClientSize = new Size(380, 204 + linhas * 54),
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true,
                ShowInTaskbar = false,
                Text = "Hora da água!"
            };
            Tema.Moldar(popup);

            popup.Controls.Add(new PictureBox { Image = Icones.Gota(44, Tema.Azul), Location = new Point(168, 20), Size = new Size(44, 44) });
            popup.Controls.Add(Rotulo("Hora de beber água!", 0, 70, 380, 30, Tema.Titulo, Tema.Texto, ContentAlignment.MiddleCenter));
            popup.Controls.Add(Rotulo($"{Configuracoes.Nome}, hoje: {litrosConsumidos:F2} de {litrosRecomendados:F2} L",
                0, 102, 380, 24, Tema.Normal, Tema.TextoSuave, ContentAlignment.MiddleCenter));

            for (int i = 0; i < slots.Count; i++)
            {
                int ml = slots[i];
                var b = new BotaoFlat
                {
                    Text = $"+{ml} ml", Icone = Icones.Recipiente(ml, 22, Tema.Fundo),
                    Location = new Point(15 + (i % 2) * 185, 140 + (i / 2) * 54), Size = new Size(175, 46)
                };
                b.Click += (_, _) => { Registrar(ml); popup.Close(); };
                popup.Controls.Add(b);
            }

            var btnDepois = new BotaoFlat
            {
                Text = "AGORA NÃO", Contorno = true, Cor = Tema.TextoSuave,
                Location = new Point(15, 144 + linhas * 54), Size = new Size(350, 40)
            };
            btnDepois.Click += (_, _) => popup.Close();
            popup.Controls.Add(btnDepois);

            popup.FormClosed += (_, _) => popupAberto = false;
            popup.Show();
            popup.Activate();
        }

        // =============== JANELA / BANDEJA ===============

        private void MostrarJanela()
        {
            VerificarNovoDia();
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        // Clicar no X apenas esconde o app (ele continua lembrando pela bandeja)
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!sair && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            base.OnFormClosed(e);
        }
    }
}
