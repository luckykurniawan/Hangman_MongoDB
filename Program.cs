//Note to self if I ever going to have anything to do with this project again
//Install MongoDB.Bson, Driver & Driver.Core via nuget package manager

namespace Hangman_MongoDB
{
    class Program
    {
        static void Main(string[] args)
        {
            Hangman hm = new Hangman();
            hm.Setup();
            hm.Start();
        }
    }
}
