using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WeatherAPI;

namespace WeatherAggregator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Weather Bot");
            WeatherBot bot = new WeatherBot();
            string[] locations = { "Charleston, SC", "Columbia, SC", "Las Vegas, NV" };
            foreach(string location in locations)
            {
                Console.WriteLine("Today's High Temp in {0} is {1}.", location, bot.GetTodaysHighTemp(location));
            }
        }
    }
}
