using System;
using System.Collections.Generic;
using System.Linq;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using System.Threading;
using Tweetinvi.Core.Extensions; // Extension methods provided by Tweetinvi


namespace TwitterSearch
{
    public interface TwitterSearchI
    {
        Searchinfo searchInfomation { get; set; }
        TwitterLocationI Location { get; set; }
        int run();
        void onResume();
        bool deleteNotify { get; set; }
        bool delete();
        void updateOnFinish(); // to up date the search after each search
        List<List<double>> loadTinyTweets(DateTime start, DateTime end, int colorId, int algorithmId = 0);
        DateTime? loadLatestLocationEffectiveDate(TwitterSearchI tsi, int algorithmId = 0);
    }
    /// <summary>
    /// search information
    /// </summary>
    public class Searchinfo
    {
        public List<String> include= new List<String>();
        public List<String> exclude = new List<String>();
        public int searchId = -1;
        public String searchTitle=null;
        public DateTime startDate;
        public DateTime? endDate = null;   // if stopped or deleted or it will be null
        public DateTime? firstTwitterDate = null;
        public DateTime? tempLastTwitterDate = null;
        public DateTime? finishedLastTwitterDate = null;
        public List<TwitterAnalyze> AnalyzeList= new List<TwitterAnalyze>();
        public int status = 0; // 0 not decide; 1 running;  2 stopped
        public bool deleted = false;
        public long tempLastTwitterId = -1;
        public bool needResume = false;
        public long finishedLastTwitterId = -1;
        public long maxIdOnResume = -1;
        public Searchinfo(int searchId, String searchTitle ,bool deleted,int searchStatus, List<String> include , List<String> exclude,DateTime startDate , DateTime? endDate , DateTime? firstTwitterDate , DateTime? tempLastTwitterDate ,long tempLastTwitterId, DateTime? finishedLastTwitterDate, long finishedLastTwitterId)
        {
            this.searchId = searchId;
            this.searchTitle = searchTitle;
            this.deleted=deleted;
            this.status= searchStatus;
            this.include = include;
            this.exclude=exclude;
            this.startDate=startDate;
            this.endDate=endDate;
            this.firstTwitterDate = firstTwitterDate;
            this.finishedLastTwitterDate = finishedLastTwitterDate;
            this.finishedLastTwitterId = finishedLastTwitterId;
            this.tempLastTwitterDate = tempLastTwitterDate;
            this.tempLastTwitterId = tempLastTwitterId;   
        }
        public Searchinfo()
        {

        }
    }

  

    public class TwitterSearch : TwitterSearchI
    {
        
        private TwitterLocationI locationR;
        public TwitterLocationI Location
        {
            get { return locationR; }
            set { locationR = value; }
        }
        public DBI dataBase;
        public Searchinfo searchInfo = new Searchinfo();
        public Searchinfo searchInfomation
        {
            get
            {
                return searchInfo;
            }
            set
            {
                searchInfo = value;
            }
        }

        
        public bool deleteNotify
        {
            get
            {
                return searchInfomation.deleted;
            }
            set
            {
                searchInfomation.deleted = value;
            }
        }
        public TwitterSearch(List<String> i, List<String> e, String searchTitle)
        {
            this.searchInfo.include = i;
            this.searchInfo.exclude = e;
            this.searchInfo.searchTitle = searchTitle;
            searchInfo.startDate = DateTime.Now;
            this.searchInfo.deleted = false;
            this.dataBase = new MysqlHelper();
            searchInfo.status = 2;
            searchInfo.searchId = dataBase.saveNewSearch(this);

        }
        public TwitterSearch(Searchinfo si) // for resume
        {
            this.dataBase = new MysqlHelper();
            this.searchInfo = si;
            onResume(); 
        }

        public int  run()
        {
            #region prepare
            System.Diagnostics.Debug.WriteLine("Search "+ searchInfo.searchId);
            TimeSpan sleepTime = config.SearchWaitInterval;
            String para = "";
            foreach (String s in searchInfo.include)
            {
                para += s;
                para += " ";
            }
            foreach (String s in searchInfo.exclude)
            {
                para += "-";
                para += s;
                para += " ";
            }
            para = para.Substring(0, para.Length - 1);
            var searchParameter = Search.CreateTweetSearchParameter(para);
            searchParameter.TweetSearchType = TweetSearchType.OriginalTweetsOnly;
            searchParameter.MaximumNumberOfResults = config.searchMaximumNumberOfResults;

            bool finished = false;
            int step = 0;
            long tempMaxId = -1;
            int cycelNumber = 0;



            if (searchInfo.status ==1)
            {
                tempMaxId = searchInfo.maxIdOnResume - 1;
            }

            if (searchInfomation.finishedLastTwitterId > 0)
            {
                searchParameter.SinceId = searchInfomation.finishedLastTwitterId + 1;
            }

            #endregion
            
            while (!finished)
            {
                if (cycelNumber++ >= config.searchCycleNumber)
                {
                    searchInfo.maxIdOnResume = tempMaxId;
                    return 0;
                }
                searchInfo.status = 1;
                dataBase.updateSearchStatus(this);
                if (tempMaxId>0)
                {
                    searchParameter.MaxId = tempMaxId;
                }
                if (deleteNotify == true)//check if this search is deleted before a long running part of this code
                {
                    return -1;
                }
                ISearchResult tweets = null;
                try
                {
                    tweets = Search.SearchTweetsWithMetadata(searchParameter);
                   
                }
                catch (System.Net.WebException ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error");
                    throw ex;
                }      
                if (!tweets.Tweets.IsEmpty())
                {
                    step = 0;//reset step
                    if (searchInfomation.finishedLastTwitterId == -1)
                    {
                        if(searchInfo.firstTwitterDate==null)
                        searchInfo.firstTwitterDate = tweets.Tweets.Last().CreatedAt;
                        if (searchInfo.firstTwitterDate> tweets.Tweets.Last().CreatedAt)
                        {
                            searchInfo.firstTwitterDate = tweets.Tweets.Last().CreatedAt;
                        }
                    }
                    if (tempMaxId < 0)
                    {
                        searchInfo.tempLastTwitterDate = tweets.Tweets.First().CreatedAt;
                        searchInfomation.tempLastTwitterId = tweets.Tweets.First().Id; // the end of twitter of this search occurs at the first cycle
                        updateOnTemp();
                    }
                    tempMaxId = tweets.Tweets.Last().Id - 1;  
                    if (deleteNotify == true)//check if this search is deleted before a long running part of this code
                    {
                        return -1;
                    }
                    Twitters tws = new Twitters(this, tweets, dataBase);
                    tws.saveBasicTweets();
                    System.Diagnostics.Debug.WriteLine("finished"+cycelNumber + " cycle");
                     
                }
                #region finished  
                if (tweets.Tweets.IsEmpty() && tweets.NumberOfQueriesUsedToCompleteTheSearch != 0)
                {
                    searchInfo.status = 2;
                   
                    searchInfo.finishedLastTwitterId = searchInfo.tempLastTwitterId;
                    searchInfo.finishedLastTwitterDate = searchInfo.tempLastTwitterDate;
                    updateOnFinish(); // will update 
                    return 1; // finished
                }
                #endregion
                if (tweets.NumberOfQueriesUsedToCompleteTheSearch == 0)
                {
                    step++;
                    if (step == 2)
                    {
                        return -1; //error 
                    }
                    System.Diagnostics.Debug.WriteLine("waiting");
                    Thread.Sleep(sleepTime);
                }
                else
                {
                    step = 0;
                }             
            }  
            return 1;
        }
       
        public bool delete()
        {
            return (dataBase.deleteSearch(this));
        }

        public void updateOnFinish()
        {
            dataBase.updateSearch(this);
        }
        public void updateOnTemp()
        {
            dataBase.updateSearch(this);
        }
        public void onResume()
        {
            if (searchInfomation.status == 1)//if it is stopped when it is running 
            {
                // get the twitter with biggest id which has only possibility to be the last inserted tweet by this search, then make sure it is from this search if not do nothing
                dataBase.updateSearchOnResume(this);
               
            }   
        }
        public List<List<double>> loadTinyTweets(DateTime start, DateTime end,int colorId,int algorithmId = 0)
        {
           return  dataBase.loadTinyTweets(this,start,end, colorId, algorithmId);
        }
        public DateTime? loadLatestLocationEffectiveDate(TwitterSearchI search, int algorithmId = 0)
        {
            return dataBase.loadLatestLocationEffectiveDate(this,0);
        }


    }
}


/*
        
                   int i = 0;
                   while (true)
                   {
                       if (i++ >= config.searchWrongNumber)
                       {
                           return -1;
                       }
                       searchMu = new Mutex();
                       bool occur = true;
                       System.Timers.Timer timer = new System.Timers.Timer(config.searchIntervalQuato);
                       manualEvent.Reset();
                       var t = new Thread(() => searchThread(ref tweets, searchParameter,ref occur));
                       timer.Elapsed += (sender, e) =>  OnTimedEvent(sender , e , t ,ref  occur);
                       timer.Start();
                       t.Start();
                       manualEvent.WaitOne();

                       if (!occur)
                           break;                  
                   }      
                  
private void searchThread(ref ISearchResult rt, ISearchTweetsParameters tp, ref bool occur)
{

    rt = Search.SearchTweetsWithMetadata(tp);
    searchMu.WaitOne();
    occur = false;
    searchMu.ReleaseMutex();
    manualEvent.Set();

}
private void OnTimedEvent(object sender, ElapsedEventArgs e, Thread source, ref bool occur)
{
    searchMu.WaitOne();
    if (!occur)
    {
        return;

    }
    source.Abort();
    occur = true;
    searchMu.ReleaseMutex();
    manualEvent.Set();
    return;
}
        */
