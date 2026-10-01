namespace LembreteAgua;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Mutex = "trava" do Windows. Se já existe uma cópia do app aberta,
        // não deixamos abrir outra (evita lembretes duplicados).
        using var mutex = new Mutex(true, "LembreteAgua_InstanciaUnica", out bool primeiraInstancia);
        if (!primeiraInstancia)
        {
            MessageBox.Show("O Lembrete de Água já está aberto (veja o ícone perto do relógio).",
                "Lembrete de Água", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}
