using System;
using System.Threading;
/*
 * algorithm id from 0 ....
 * search status 0 not decided 1 running 2 stopped
 * search deleted true deleted, false not deleted
 * realtwitterId from 1 as it is auto increment
 * */
namespace TwitterSearch
{
    public static  class config
    {
    
        private static TimeSpan sec5 = new TimeSpan(0,0,5);
        private static TimeSpan min5 = new TimeSpan(0,5,0);
        private static TimeSpan min30 = new TimeSpan(0, 30, 0);
        private static TimeSpan h24 = new TimeSpan(24,0,0);
        private static TimeSpan min10 = new TimeSpan(0,10,0);
        public static TimeSpan DemoSearchCheckInterval = h24;
        public static TimeSpan DemoSearchWaitInterval = min5;
        public static TimeSpan SearchWaitInterval = min5 + min5 + min5;
        public static TimeSpan googleLocationCheckInterval = min5;
        public static TimeSpan googlelocationWaitInterval = min5;
        public static TimeSpan googlelocationWaitIntervalWhenLimit = h24;
        public static TimeSpan googlelocationWaitIntervalWhenExecption = sec5;
        public static double searchIntervalQuato = 1000 * 60 * 10; // for each search cycle how long it can take
        public static int googleLocationTweetNumberEachCycle = 200; // will laod 200 tweets to handel each time
        public static int searchCycleNumber = 10; // max search cycle each search can take
        public static int searchWrongNumber = 3; //if waiting too long for the search to finish abort then do it again, this number indicate how many wrong time to allow
        public static int analyzeTwitterLoaderCount = 2000; // number for each search load max random twitters
        public static int googleLocationTweetNumberEachCycleTest = 200;
        public static int searchMaximumNumberOfResults = 1000;
        public static bool debug = true;
        public static TimeSpan[] dataBaseConnExecptionWaitInterval = {sec5,min5,min30, new TimeSpan(24, 0, 0)};
        public static Mutex mu = new Mutex();
        public static void mailError(Exception e)
        {
            mu.WaitOne();


            mu.ReleaseMutex();
        }
    }
}
