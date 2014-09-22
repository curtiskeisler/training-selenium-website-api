using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SeleniumWrapper;

namespace WeatherAPI
{
    public class WeatherBot: WebBot
    {
        /// <summary>
        /// Get today's high temperature from the Weather Channel
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public string GetTodaysHighTemp(string location)
        {
            string todaysHigh = "Unable to retrieve. Try again later.";
            try
            {
                OpenBrower();
                Navigate("http://www.weather.com");
                Wait(1000);
                FillTextBox("where", location);
                Wait(2000);
                ClickXPath(@"//*[@id=""headerSearchForm""]/button");
                Wait(5000);
                todaysHigh = GetTextAtXPath( @"//*[@id=""wx-forecast-container""]/div[1]/div[2]/div[7]/div[1]/span[1]");
                CloseBrowser();
                Console.WriteLine(GetLog());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
                Console.WriteLine("Here's the log");
                Console.WriteLine(GetLog());
            }
            return todaysHigh;
        }
    }
}
