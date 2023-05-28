using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Hangman_MongoDB
{
    class Hangman
    {
        MongoClientSettings settings;
        MongoClient client;
        IMongoDatabase database;
        bool gameLoopFlag = true;

        const int maxWrong = 8;

        public Hangman(){}

        public void Setup()
        {
            //connect to database
            settings = MongoClientSettings.FromConnectionString("*deleted*"); //copied from how to connect in mongodb database
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            client = new MongoClient(settings);
            try
            {
                client.ListDatabaseNames();
            }
            catch (Exception)
            {
            }

            //Console.WriteLine( client.Cluster.Description.State == ClusterState.Connected);
            
            //get the word database
            database = client.GetDatabase("word_database");
        }

        public void Start()
        {
            while (gameLoopFlag)
            {
                Console.WriteLine("Hangman\n");
                ShowMenu();
            }
        }

        void Play()
        {
            //get the number of words in database
            var collection = database.GetCollection<BsonDocument>("words");
            var docNumber = collection.CountDocuments(new BsonDocument());

            bool isPlaying = true;

            while (isPlaying)
            {
                //get starting time
                DateTime startTime = DateTime.Now;
                Console.Clear();
                //get random word from database
                Random rand = new Random();
                int num = rand.Next(1, (int)docNumber);
                //Console.WriteLine(num);

                var result = collection.Find("{id:" + num + "}").FirstOrDefault();
                string mysteryWord = result.GetValue("word").ToString();
                //Console.WriteLine(mysteryWord);
                string guessSoFar = new string('*', mysteryWord.Length);

                int wrongCount = 0; //counter for wrong guesses
                int charCounter = 0; //counter for letter occurences in the word
                char playerGuess; //variable for player input

                string usedChars = ""; //variable to contain letters used

                //gameloop
                while (wrongCount < maxWrong && guessSoFar != mysteryWord)
                {
                    Console.Clear();

                    Console.WriteLine("You have " + (maxWrong - wrongCount) + " guesses.");

                    Console.Write("The word so far: ");
                    Console.WriteLine(guessSoFar);

                    Console.Write("Used letters:");
                    Console.Write(usedChars);

                    Console.WriteLine();

                    Console.Write("Enter your guess: ");
                    if (char.TryParse(Console.ReadLine(), out playerGuess))
                    {
                        playerGuess = char.ToLower(playerGuess);
                        // if input is a number or letter already used
                        while (usedChars.Contains(playerGuess) || Char.IsDigit(playerGuess))
                        {
                            if(Char.IsDigit(playerGuess))
                                Console.WriteLine("Please input alphabet only.");
                            else
                                Console.WriteLine("You have already guessed " + playerGuess);
                            Console.Write("Please enter your guess: ");
                            char.TryParse(Console.ReadLine(), out playerGuess);
                        }
                        usedChars += playerGuess;        
                        Console.WriteLine();
                        //letter is not in word
                        if (!mysteryWord.Contains(playerGuess))
                        {
                            wrongCount++;
                            Console.WriteLine("No letter " + playerGuess + "in the word.");
                        }
                        //letter is in word
                        else
                        {
                            charCounter = 0;
                            char[] charArr = guessSoFar.ToCharArray();
                            for (int i = 0; i < mysteryWord.Length; i++)
                            {
                                if (playerGuess == mysteryWord[i])
                                {
                                    charArr[i] = playerGuess;
                                    charCounter++;
                                }
                            }
                            guessSoFar = new string(charArr);
                            Console.WriteLine("Found " + charCounter + " letter " + playerGuess + ".");
                        }
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("Please enter only 1 character at a time.");
                    }
                }

                Console.Clear(); 

                //show result
                Console.Write("Mystery word: ");
                Console.WriteLine(mysteryWord);
                Console.Write("Your guess: ");
                Console.WriteLine(guessSoFar);

                if (wrongCount == maxWrong)
                    Console.WriteLine("You failed to guess the word.");            
                else
                    Console.WriteLine("You guessed the word.");

                //get end time
                DateTime endTime = DateTime.Now;
                TimeSpan playtime = endTime - startTime;
                //Console.WriteLine(startTime.ToString());
                //Console.WriteLine((int)playtime.TotalSeconds);
                //insert result into database
                var resultCollection = database.GetCollection<BsonDocument>("results");
                string finalGuess = guessSoFar;
                //Console.WriteLine(finalGuess);
                var document = new BsonDocument
                {
                    { "start_time", startTime.ToString() },
                    { "mystery_word", mysteryWord },
                    { "player_guess", finalGuess },
                    { "play_time", (int) playtime.TotalSeconds },
                };

                resultCollection.InsertOne(document);

                if (AskYesNo("Do you want to play again") == 'n') isPlaying = false;
                Console.Clear();
            }
        }

        void UploadWordCollection()
        {
            //insert words to database
            //https://stackoverflow.com/questions/816566/how-do-you-get-the-current-project-directory-from-c-sharp-code-when-creating-a-c
            // This will get the current WORKING directory (i.e. \bin\Debug)
            string workingDirectory = Environment.CurrentDirectory;
            // This will get the current PROJECT directory
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
            string path = Path.Combine(projectDirectory, "dictionary.txt");
            int count = 0;
            foreach (string line in File.ReadLines(path, Encoding.UTF8))
            {
                count++;
                //if (count > 1) break;
                var document = new BsonDocument
                {
                    { "id", count },
                    { "word", line }
                };
                var collection = database.GetCollection<BsonDocument>("words");
                collection.InsertOne(document);
                //Console.WriteLine(line);

            }
        }

        public void ShowMenu()
        {
            Console.WriteLine("Menu\n1 - Play\n2 - Show Playing History\n3 - Exit");
            int choice = 0;
            Console.Write("Choice: ");
            bool success = int.TryParse(Console.ReadLine(), out choice);
            while (!success || choice < 1 || choice > 3)
            {

                if (!success)
                {
                    Console.WriteLine("Please enter only number.");
                }
                else
                {
                    Console.WriteLine("Please enter number only between 1 and 3");
                }
                Console.Write("Choice: ");
                success = int.TryParse(Console.ReadLine(), out choice);
            }

            switch (choice)
            {
                case 1:
                    Play();
                    break;
                case 2:
                    ShowPlayHistory();
                    break;
                case 3:
                    gameLoopFlag = false;
                    break;
                default:
                    break;
            }
        }

        void ShowPlayHistory()
        {
            Console.Clear();
            //show 10 fastest games where player won
            var collection = database.GetCollection<BsonDocument>("results");
            //filtering the result where player won (mystery word is the same as player guess)
            string filter = "{$expr:{$eq:[\"$mystery_word\",\"$player_guess\"]}}";
            //Console.WriteLine(filter);
            var results = collection.Find(filter).Sort("{play_time:1}").Limit(10).ToList();
            //var result = collection.Find("$mystery_word === $player_guess}");
            Console.WriteLine("===== Fastest 10 Games =====");
            Console.Write("{0,-10}\t{1,-20}\t{2,-10}\n", "Date", "Word", "Play Time");
            Console.WriteLine("=====================================================");
            foreach (var result in results)
            {
                //Console.WriteLine(r);
                DateTime date = Convert.ToDateTime(result.GetValue("start_time"));
                String word = Convert.ToString(result.GetValue("player_guess"));
                int time = Convert.ToInt32(result.GetValue("play_time"));
                //Console.Write(date.ToShortDateString() + "\t" + word + "\t\t" + time + "\n");
                Console.Write("{0,-10}\t{1,-20}\t{2,-5} seconds\n", date.ToShortDateString(), word, time);
            }
            Console.WriteLine();

            //get last 10 games result
            results = collection.Find(new BsonDocument()).Sort("{_id:-1}").Limit(10).ToList();
            Console.WriteLine("======= Last 10 Games =======");
            Console.Write("{0,-20}\t{1,-20}\t{2,-20}\t{3,-10}\n", "Play Date", "Word", "Player Guess", "Play Time");
            Console.WriteLine("=====================================================================================");
            foreach (var result in results)
            {
                DateTime date = Convert.ToDateTime(result.GetValue("start_time"));
                String mystery_word = Convert.ToString(result.GetValue("mystery_word"));
                String player_guess = Convert.ToString(result.GetValue("player_guess"));
                int time = Convert.ToInt32(result.GetValue("play_time"));
                Console.Write("{0,-20}\t{1,-20}\t{2,-20}\t{3,-5} seconds\n", date.ToString(), mystery_word, player_guess, time);
            }
            Console.WriteLine();

            ShowMenu();
        }

        char AskYesNo(string question)
        {
            char answer;
            do
            {
                Console.Write(question + " (y/n) : ");
                char.TryParse(Console.ReadLine(), out answer);
                answer = Char.ToLower(answer);
            } while (answer != 'y' && answer != 'n');
            return answer;
        }
    }
}
