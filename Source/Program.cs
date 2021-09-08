using System;

namespace NatChecker
{
    class Program
    {
        static NetworkManager manager;
        static void Main(string[] args)
        {

            manager = new NetworkManager();
            manager.StartInit();
            manager.EndInit();
            if (!manager.InternetConnection)
            {
                Console.WriteLine("No internet connection!");
                return;
            }
            Console.WriteLine("Your ip is: "+manager.PublicIP);
            if (manager.TypeOfNat == NatType.Compatible)
            {
                Console.WriteLine("Yey! Your Nat is compatible with UDP hole punching!");
            }
            else
            {
                Console.WriteLine("Your Nat is not compatible with UDP hole punching");
            }
            manager.Dispose();
            Console.ReadKey();
        }
    }
}
