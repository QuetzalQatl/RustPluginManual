namespace Oxide.Plugins
{
    [Info("HelloWorldCs", "Bas", "0.1.0")]
    class CsHelloWorld : RustPlugin
    {
        void Init()
        {
            System.Console.WriteLine("Hello World from Cs");
        }
    }
}
