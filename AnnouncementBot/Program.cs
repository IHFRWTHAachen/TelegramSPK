using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Speech.Synthesis;
using System.IO;
using System.Diagnostics;
using System.Threading;
using ATL;

namespace AnnouncementBot
{
    class Program
    {
        private static Boolean m_Close = false;

        private static SpeechSynthesizer m_Synthesizer;

        static void Main(string[] args)
        {
            Log("Bot has been started");
            InitializeTTSEngine();
            Log("Text-to-Speech engine has been initialized");
            StartServer();
            ConsoleKeyInfo info = Console.ReadKey();
            if (info.Key == ConsoleKey.Escape)
            {
                m_Close = true;
            }
        }

        private static void InitializeTTSEngine()
        {
            m_Synthesizer = new SpeechSynthesizer();
            m_Synthesizer.SetOutputToDefaultAudioDevice();
            m_Synthesizer.Rate = 0;
            m_Synthesizer.Volume = 100;
            m_Synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
            //m_Synthesizer.SelectVoice("LH Anna");
            
        }

        private static void Log(String message)
        {
            Console.WriteLine(string.Format("{0}: {1}", DateTime.Now.ToString(), message));
        }

        private async static void StartServer()
        {
            try
            {
                TelegramBotClient client = new TelegramBotClient("269007176:AAFGjCV3qxbzLw0h4rVREGznLzJWXEFJWa0");
                User user = await client.GetMeAsync();
                Log("Bot is online");

                int offset = 0;
                while (!m_Close)
                {
                    Update[] updates = client.GetUpdatesAsync(offset).Result;
                    foreach(Update update in updates)
                    {
                        int index = -1;
                        if (update.Message.Text != null && (index = update.Message.Text.ToLower().IndexOf("durchsage:")) != -1)
                        {
                            string message = update.Message.Text.Substring(index + 10).Trim();
                            Log(string.Format("New Announcement ({0}): {1}", update.Message.From.FirstName, message));

                            m_Synthesizer.Speak(string.Format("Achtung, eine Durchsage: {0}", message));
                        }
                        else if (update.Message.Type == Telegram.Bot.Types.Enums.MessageType.VoiceMessage)
                        {
                            string fileID = update.Message.Voice.FileId;
                            MemoryStream buffer = new MemoryStream();
                            Telegram.Bot.Types.File file = await client.GetFileAsync(fileID, buffer);
                            buffer.Position = 0;

                            // Debug: Write to File
                            if (System.IO.File.Exists("Test.ogg"))
                                System.IO.File.Delete("Test.ogg");
                            FileStream fileStream = new FileStream("Test.ogg", FileMode.CreateNew, FileAccess.Write);
                            fileStream.Write(buffer.GetBuffer(), 0, (Int32)buffer.Length);
                            fileStream.Flush();
                            fileStream.Close();

                            ATL.AudioReaders.IAudioDataReader audioReader = ATL.AudioReaders.AudioReaderFactory.GetInstance().GetDataReader("Test.ogg");
                            audioReader.ReadFromFile("Test.ogg");
                            Int32 duration = (Int32)audioReader.Duration;

                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = "C:\\Program Files (x86)\\VideoLAN\\VLC\\vlc.exe";
                            startInfo.Arguments = "C:\\Users\\Roland\\OneDrive\\Projects\\AnnouncementBot\\AnnouncementBot\\bin\\Debug\\Test.ogg";
                            Process process = Process.Start(startInfo);
                            Thread.Sleep((duration + 5) * 1000);
                            process.Kill();   
                        }


                        offset = update.Id + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Exception caught: " + ex.Message);
            }
        }
    }
}
