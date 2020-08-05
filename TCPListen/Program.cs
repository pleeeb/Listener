using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;

namespace TCPListen
{
    class Program
    {
        static void Main(string[] args)
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("user-data-dir=C:\\Users\\Pete\\AppData\\Local\\Google\\Chrome\\App");
            IWebDriver driver = new ChromeDriver(options);
            driver.Url = "https://twitch.tv";

            TcpListener server = null;

            try
            {
                int port = 13215;
                IPAddress localAddr = IPAddress.Parse("192.168.0.28");

                server = new TcpListener(localAddr, port);
                server.Start();

                Byte[] bytes = new Byte[256];
                string data = null;

                

                while (true)
                {
                    Console.Write("Waiting for a connection");

                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected");

                    data = null;

                    NetworkStream stream = client.GetStream();

                    int i;

                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine("Received: {0}", data);

                        Data d = JsonConvert.DeserializeObject<Data>(data);

                        Console.WriteLine(d.Command);
                        Console.WriteLine(d.Name);
                        Console.WriteLine(d.Volume);
                        Console.WriteLine(d.FullScreen);

                        webPageActions(driver, d);

                        data = data.ToUpper();

                        byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                        stream.Write(msg, 0, msg.Length);
                        Console.WriteLine("Sent: {0}", data);
                    }

                    //client.Close();
                    //Console.WriteLine("Connection Closed");
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally {
                server.Stop();
            }
        }

        public static void webPageActions(IWebDriver driver, Data d) {
            switch (d.Command)
            {
                case 0:
                    Process.Start(new ProcessStartInfo("shutdown", "/s /t 0")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    break;
                case 1:
                    driver.Navigate().GoToUrl("https://twitch.tv/" + d.Name.Substring(1, d.Name.Length - 2));
                    break;
                case 2:
                    IJavaScriptExecutor jse = (IJavaScriptExecutor)driver;
                    String changeV = "document.getElementsByTagName('video')[0].volume=" + d.Volume.ToString();
                    jse.ExecuteScript(changeV);
                    break;
                case 3:
                    driver.FindElement(By.XPath("//button[contains(@aria-label,\'Fullscreen (f)')]")).Click();
                    break;
                case 4:
                    driver.Navigate().Refresh();
                    if (d.FullScreen == true)
                    {
                        driver.FindElement(By.XPath("//button[contains(@aria-label,\'Fullscreen (f)')]")).Click();
                    }
                    break;
            }
        }
    }
}
