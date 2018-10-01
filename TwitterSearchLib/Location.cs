using System;
using System.Collections.Generic;
using System.Threading;
using TwitterSearchLib.locationAlgorithm;
namespace TwitterSearch
{
    /*
     * algorithm list, need to make sure the calling sequence same with the id of algorithm in database
     * 0  googlemap
     * 1  1Method
     */
    /*
     * to add new algorithm 
     * 1. check tweet table to see is index after La Lo city... enough, if not alter the table to add La+Id Lo+Id city+id like La3 Lo3 city3 ... for fourth algorithm 
     * 2. need to modify database to insert new record into algorithm table 
     * by "insert into algorithm (algorithmId,algorithmName,lastMaxId) values ({type algorithmId},{type Name},0);"
     * 3. need to modify run() and add algorithm schedule function() in this file
     * 4. add a class to do the job of calcing location in folder locationAlgorithm which need to implement LocationHelper interface
     * */


    public interface TwitterLocationI
    {
        List<AlgorithmInfo> algorithmInfomations { get; set; }
        void run();
    }
    public interface TwitterLocationInnerI
    {
        void innitialize();
    }
    /*
     * pass in Twitter list and algorithmId, for each twitter use the method to calc location 
     * and create  LocationModel then add it to this twitter, at last return the last twitter u handled
     * */
    public interface LocationHelper
    {
        TwitterModel run(TwittersI tsList, int algorithmId);
    }
    public class AlgorithmInfo
    {
        public int algorithmId= -1;
        public long lastMaxId=-1;
        public String algorithmsName;
        public DateTime? lastDate = null;
        public AlgorithmInfo( long lastMaxId, String algorithmsName,DateTime? last, int id)
        {
            this.algorithmId = id;
            this.lastMaxId = lastMaxId;
            this.algorithmsName = algorithmsName;
            this.lastDate = last;
        }
    }
    public  class TwitterLocation : TwitterLocationI, TwitterLocationInnerI
    { 
        private static TimeSpan sleepInterval = config.googlelocationWaitInterval;
        private static TimeSpan  checkInterval= config.googleLocationCheckInterval;
        int TweetbnumberEachCycle = config.googleLocationTweetNumberEachCycle;
        protected DBI database;
        private List<AlgorithmInfo> algorithmInfomationsR = new List<AlgorithmInfo>();
        public List<AlgorithmInfo> algorithmInfomations
        {
            get
            {
                return this.algorithmInfomationsR;
            }
            set
            {
                this.algorithmInfomationsR = value;
            }
        }
       
        public TwitterLocation()
        {
             database = new MysqlHelper();
             innitialize();
            
        }
        public void innitialize()
        {
            algorithmInfomations = database.loadAlgorithmInfo();
        }
       

        public void run()
        {
            /* to add new algorithm mimic bellow, 
            *  but first need to modify database to insert new record into algorithm table 
            *  by "insert into algorithm (algorithmId,algorithmName,lastMaxId) values ({type algorithmId},{type Name},0);"
            * */
            Thread td0 =  new Thread(()=>GoogleMap(0));
            td0.Start();
        }

        #region location Algorithm bodies 
        //algorithm 0
        //for new algorthm need mimic bellow and replace GoogleMapHelper() to ur new function
        public void GoogleMap(int AlgorithmId)
        {
            while (true)
            {
                bool continue1 = startCheck();
                while (continue1)
                {
                    TwittersI ts = new Twitters();
                    ts.allTweets = database.loadBasicTweets(algorithmInfomations[AlgorithmId].lastMaxId + 1, this.TweetbnumberEachCycle);
                    if (ts.allTweets.Count != 0)//if nothing is loaded not run
                    {
                        //for new algorthm need to replace GoogleMapHelper() to ur new function
                        LocationHelper ghp = new GoogleMapHelper();


                        TwitterModel id = ghp.run(ts, AlgorithmId);
                        algorithmInfomations[AlgorithmId].lastMaxId = id.realTwitterId;
                        algorithmInfomations[AlgorithmId].lastDate = id.createAt;
                        ts.saveLocationInTweets(AlgorithmId);
                        database.updateAlgorithmInfo(algorithmInfomations[AlgorithmId]);
                    }
                    else
                    {
                        continue1 = false;
                    }

                }
                Thread.Sleep(sleepInterval); // wait for sleepInterval, then continue
            }
        }
        //algorithm 1 ....






        #endregion
        public bool startCheck() // it will start when the latest tweet in tweets table is older than checkInterval, then it will run till it processed all tweets in tweets table
        {
            DateTime? dt = database.getLastTwitterDate();

            if (dt!=null&&(DateTime.Now - dt) > checkInterval)
            {
                return true;
            }

            return false;
        }
        
        
       


    }
   
   
}
