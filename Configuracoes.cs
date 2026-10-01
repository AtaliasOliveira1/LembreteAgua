using System;
using System.Globalization;
using Microsoft.Win32;
using System.Windows.Forms;

namespace LembreteAgua
{
    public static class Configuracoes
    {
        private const string CaminhoRegistro = @"SOFTWARE\LembreteAgua";
        private const string CaminhoInicializacao = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string NomeApp = "LembreteAgua";
        private static readonly int[] CoposPadrao = { 250, 300, 500, 700, 1000 };

        // Perfil
        public static string Nome { get; set; } = "Turquia";
        public static double Peso { get; set; } = 70;
        public static double Altura { get; set; } = 175;
        public static int IntervaloMinutos { get; set; } = 60;
        public static bool IniciarComWindows { get; set; } = false;
        public static bool AutoIniciarLembretes { get; set; } = false;
        public static bool Configurado { get; set; } = false;   // NOVO: já salvou alguma vez?

        // Copos
        public static int QuantidadeSlots { get; set; } = 1;
        public static int[] CoposMl { get; set; } = (int[])CoposPadrao.Clone();

        // NOVO: consumo do dia fica salvo, então não zera se você fechar o app
        public static double ConsumoHoje { get; set; } = 0;
        public static string DataConsumo { get; set; } = "";

        private static string Hoje => DateTime.Today.ToString("yyyy-MM-dd");

        public static void Carregar()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(CaminhoRegistro))
                {
                    if (key != null)
                    {
                        Nome = Convert.ToString(key.GetValue("Nome", "Turquia")) ?? "Turquia";
                        Peso = Convert.ToDouble(key.GetValue("Peso", 70.0));
                        Altura = Convert.ToDouble(key.GetValue("Altura", 175.0));
                        IntervaloMinutos = Convert.ToInt32(key.GetValue("IntervaloMinutos", 60));
                        QuantidadeSlots = Convert.ToInt32(key.GetValue("QuantidadeSlots", 1));
                        AutoIniciarLembretes = Convert.ToBoolean(key.GetValue("AutoIniciarLembretes", false));
                        Configurado = Convert.ToBoolean(key.GetValue("Configurado", false));

                        for (int i = 0; i < 5; i++)
                            CoposMl[i] = Convert.ToInt32(key.GetValue($"CopoSlot{i}", CoposPadrao[i]));

                        DataConsumo = Convert.ToString(key.GetValue("DataConsumo", "")) ?? "";
                        double.TryParse(Convert.ToString(key.GetValue("ConsumoHoje", "0")),
                            NumberStyles.Float, CultureInfo.InvariantCulture, out double consumo);
                        ConsumoHoje = consumo;
                    }
                }

                using (RegistryKey? keyStartup = Registry.CurrentUser.OpenSubKey(CaminhoInicializacao))
                {
                    if (keyStartup != null)
                        IniciarComWindows = keyStartup.GetValue(NomeApp) != null;
                }
            }
            catch { /* mantém os valores padrão se der erro de leitura */ }

            QuantidadeSlots = Math.Clamp(QuantidadeSlots, 1, 5);
            NovoDiaSeNecessario();
        }

        public static void Salvar()
        {
            try
            {
                Configurado = true;
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(CaminhoRegistro))
                {
                    key.SetValue("Nome", Nome);
                    key.SetValue("Peso", Peso);
                    key.SetValue("Altura", Altura);
                    key.SetValue("IntervaloMinutos", IntervaloMinutos);
                    key.SetValue("QuantidadeSlots", QuantidadeSlots);
                    key.SetValue("AutoIniciarLembretes", AutoIniciarLembretes);
                    key.SetValue("Configurado", Configurado);
                    for (int i = 0; i < 5; i++)
                        key.SetValue($"CopoSlot{i}", CoposMl[i]);
                }

                using (RegistryKey? keyStartup = Registry.CurrentUser.OpenSubKey(CaminhoInicializacao, true))
                {
                    if (keyStartup != null)
                    {
                        if (IniciarComWindows)
                            // Aspas no caminho evitam problemas se a pasta tiver espaços
                            keyStartup.SetValue(NomeApp, $"\"{Application.ExecutablePath}\"");
                        else
                            keyStartup.DeleteValue(NomeApp, false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar no Registro: " + ex.Message);
            }
        }

        // NOVO: salva só o consumo (chamado a cada copo registrado)
        public static void SalvarConsumo()
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(CaminhoRegistro);
                key.SetValue("DataConsumo", DataConsumo);
                key.SetValue("ConsumoHoje", ConsumoHoje.ToString(CultureInfo.InvariantCulture));
            }
            catch { }
        }

        // NOVO: se virou o dia, zera o consumo. Retorna true se zerou.
        public static bool NovoDiaSeNecessario()
        {
            if (DataConsumo == Hoje) return false;
            DataConsumo = Hoje;
            ConsumoHoje = 0;
            SalvarConsumo();
            return true;
        }

        public static bool EhPrimeiraVez() => !Configurado;
    }
}
