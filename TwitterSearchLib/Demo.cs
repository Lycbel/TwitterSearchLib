
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tweetinvi;

namespace TwitterSearch
{
    /// <summary>
    /// interact with web; control search and location thread;
    /// </summary>
    public interface DemoI
    {
        /// <summary>
        /// add one new search; but before this need to create the instance of TwitterSearchI
        /// </summary>
        /// <param name="ts">the instance of TwitterSearchI</param>
        /// <returns></returns>
        bool addSearch(TwitterSearchI ts);
        bool deleteSearchById(int id);
        TwitterSearchI getSearch(int id);  //return the search in runningSearches list, return null when no search in the list
        /// <summary>
        /// run the location and search threads
        /// </summary>
        void run(); 
        List<TwitterSearchI> getRunningSearches();// return all the searches which are running
        /// <summary>
        /// create new search
        /// </summary>
        /// <param name="i">list of key words need to be included</param>
        /// <param name="e">list of key words need to be exuded</param>
        /// <param name="searchTitle"></param>
        /// <returns></returns>
        TwitterSearchI createSearch(List<String> i, List<String> e, String searchTitle);
        /// <summary>
        /// create new search
        /// </summary>
        /// <param name="i">the included key words are in one string seperated by \n</param>
        /// <param name="e">the excluded key words are in one string seperated by \n</param>
        /// <param name="searchTitle"></param>
        /// <returns></returns>
        TwitterSearchI createSearch(String i, String e, String searchTitle);
        /// <summary>
        ///<para> load tweets based on infomation in ids[] includes startDate; endDate; searchIds; and each search's color id</para>
        ///<para> return the datas for google goechart api which are list of [la,lo,colorId]; and map of time offset to time; and interval and pixel scale which will define the div length of time bar</para>
        /// </summary>
        /// <param name="ids">in pattern [year1,month1,day1,hour1,year2,month2,day2,hour2,searchId1,colorId1,searchId2,colorId2,searchId3,colorId3.....]</param>
        /// <param name="algorithmId">which algorithm to use</param>
        /// <returns>in pattern of [[[la1,lo1,searchcolor1],.......],[[yearmonth1,intervaloffset1],....],[interval,pixel]]</returns>
        List<object> loadTweetsForAnalyze(string[] ids,int algorithmId = 0);
        /*
         * dates are startDate and endDate, searchLists are all searchIds, LaLos are two La lo pairs
         * by these constriants to get number of tweets of intervals which are divided from the original interval into intervalCount small intervals
         * return the list of data for google linechart
         **/
        List<List<String>> loadTweetsForAnalyzeSelection(String dates, String searchLists, String laLos,int intervalCount =20);
    }


    public class Demo : DemoI
    {
        private List<TwitterSearchI> runningSearchR = new List<TwitterSearchI>();
        private int runningPoint = -1;
        private static Mutex mu = new Mutex();
        private static TimeSpan Sinterval = config.DemoSearchCheckInterval;
        private static TimeSpan timeSleep = config.DemoSearchWaitInterval;
        #region basePart
        public Demo()
        {
            dataBase = new MysqlHelper();                   
            resume();
        }
        void doAuth()
        {
            Auth.SetUserCredentials("MWnAZKxURNa9GRQiowKL6GCNo", "EantILNaPFkjZv0tyWokKXmTIrDI92nYyrqzEAn0u606OKBpzl", "867811120895057920-nqXWRRtpApWV3uyeX3fxo6WTZUeeNrD", "nLp6YqhdRnF6LiREU7rMxgp4BKqiLeQDwjsz8YqWVRrwC");
            TweetinviEvents.QueryBeforeExecute += (sender, args) => { System.Diagnostics.Debug.WriteLine(args.QueryURL); }; //for test
            var authenticatedUser = User.GetAuthenticatedUser();
        }
        private void resume()
        {
            List<Searchinfo> sis = dataBase.getAllSearchInfo();
            foreach (Searchinfo si in sis)
            {
                TwitterSearchI TsI = new TwitterSearch(si);
                runningSearch.Add(TsI);
            }
           
        }
        public void run()
        {
            TwitterLocationI lo = new TwitterLocation();
            lo.run();
            doAuth();
            ThreadStart ts = new ThreadStart(runSearch);
            Thread td = new Thread(ts);
            td.Start();
        }
        #endregion
        #region searchPart
        public List<TwitterSearchI> runningSearch
        {
            get
            {
                return this.runningSearchR;
            }
            set
            {
                this.runningSearch = value;
            }

        }
        public DBI dataBase;
        private static Mutex mut = new Mutex();
        private void runSearch()
        {

            while (true)
            {
                bool finished = true;

                mu.WaitOne();
                //when the status is waiting and there are searches in list, we need to check i the first search finished waiting the time interval 
                if ( checkDate()!=-1)
                {
                    finished = false;
                    runningPoint = checkDate(); 
                }    
                if (runningPoint != -1) // it means there is new added search xxxxx
                {
                    finished = false;
                }
                mu.ReleaseMutex();
                while (!finished)
                {
                    mu.WaitOne();
                    if (runningPoint >= runningSearch.Count) //check if end
                    {
                        finished = true;
                        runningPoint = -1;
                    }
                    if (runningPoint >= 0) //if delete the first search when there is only one search in list and it occured before this critical section, it will cause problem
                    {
                        TwitterSearchI temps = runningSearch[runningPoint];
                        runningPoint++;
                        mu.ReleaseMutex();
                        temps.run();
                    }
                    else
                    {
                        finished = true;
                        mu.ReleaseMutex();
                    }

                }
                System.Diagnostics.Debug.WriteLine("Demo waiting");
                Thread.Sleep(timeSleep); 
            }

        }
        private int checkDate()//check searches in list to see is any need to run if yes, return the first's index
        {
          
            int i = 0;
            foreach(TwitterSearch s in runningSearch)
            {
                if (s.searchInfomation.finishedLastTwitterDate==null)
                {
                    return i;
                }
                TimeSpan ts = (DateTime.Now - (DateTime)s.searchInfomation.finishedLastTwitterDate);
                TimeSpan ts1 = ts - Sinterval;
                if ( ts > Sinterval)
                {
                    return i;
                }
                i++;
            }
            return -1;
        }
        public bool addSearch(TwitterSearchI ts)
        {
            mu.WaitOne();
            runningSearch.Add(ts); // will mutex
            if (runningPoint <= -1) //if at the rest status no work
            {
                runningPoint = runningSearchR.Count - 1;
            }
            mu.ReleaseMutex();
            return true;
        }
        public bool deleteSearchById(int id)
        {
            mu.WaitOne();
            foreach (TwitterSearchI ts in runningSearch)
            {
                if (ts.searchInfomation.searchId == id)
                {
                    int position = runningSearch.IndexOf(ts);
                    ts.deleteNotify = true;
                    ts.delete();
                    runningSearch.Remove(ts);
                    if (runningPoint > -1 && runningPoint <= position)
                    {
                        runningPoint--;
                    }
                    mu.ReleaseMutex();
                    return true;
                }
            }
            mu.ReleaseMutex();
            return false;
        }
        public TwitterSearchI getSearch(int id)
        {
            mu.WaitOne();
            TwitterSearchI tts = null;
            foreach (TwitterSearchI ts in runningSearch)
            {
                if (ts.searchInfomation.searchId == id)
                {
                    tts = ts;
                }
            }
            mu.ReleaseMutex();
            return tts;
        }
        public TwitterSearchI createSearch(List<String> i, List<String> e, String searchTitle)
        {
            return new TwitterSearch(i, e, searchTitle);
        }
        public TwitterSearchI createSearch(String i, String e, String searchTitle)

        {
            List<String> inc = null;
            if (i != null)
            {
                inc = i.Split('\n').ToList();
                inc.Remove("");
            }
            List<String> exc = null;
            if (e != null)
            {
                exc = e.Split('\n').ToList();
                exc.Remove("");
            }
            return createSearch(inc, exc, searchTitle);
        }

        public List<TwitterSearchI> getRunningSearches()
        {
            return this.runningSearch;
        }
        #endregion
        #region analyze part
        public List<object> loadTweetsForAnalyze(string[] ids, int algorithmId = 0)
        {

            if (ids.Count() < 8)
            {
                return null;
            }
           
            String[] a = ids;
            DateTime startDate;
            DateTime endDate;
            if (ids[0] == "-1" && ids[4] == "-1")//if it is the first time load tweets
            {
                DateTime? startDatet = getMaxDate(ids, this);
                DateTime? endDatet =getMinDate(ids, this);
                if(startDatet==null|| endDatet == null)
                {
                    return null;
                }
                startDate =(DateTime) startDatet;
                 endDate = (DateTime)endDatet;
                if ( (endDate - startDate) <= TimeSpan.FromHours(1)) // if there is no valid intersection return null
                {
                    return null;
                }
            }
            else// if not first time 
            {
                startDate = ConvertDate(ids[0], ids[1], ids[2], ids[3]);
                endDate = ConvertDate(ids[4], ids[5], ids[6], ids[7]);

            }

            IEnumerable<List<double>> tts = new List<List<double>>();
            for (int i = 8; i <= (Double)(ids.Count())/2 + 3; i++)
            {
                TwitterSearchI tsi = getSearch(Convert.ToInt32(ids[((i-4)*2)]));
                int colorId = Convert.ToInt32(ids[((i - 4) * 2 + 1)]);
                List <List< double> > temp = tsi.loadTinyTweets(startDate, endDate, colorId, algorithmId);
                tts = tts.Concat(temp);
            }

            List<object> finalList = new List<object>();
            List<List<double>> mm = tts.ToArray().ToList();
            finalList.Add(mm);

            if (ids[0] == "-1" && ids[4] == "-1")//if it is the first time load tweets
            {
                List<long> tempInterval = new List<long>();
                List<long[]> tempDmap = generateDateMap(startDate, endDate, ref tempInterval);
                finalList.Add(tempDmap);
                finalList.Add(tempInterval);

            }
            return finalList;

        }
        private DateTime ConvertDate(String year, String month, String date, String hour)
        {

            return new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), Convert.ToInt32(date), Convert.ToInt32(hour), 0, 0);
        }
        private DateTime? getMinDate(String[] ids, DemoI nn)
        {
            bool first = true;
            DateTime? dt = null;
            for (int i = 8; (Double)i <= (Double)(ids.Count()) / 2 + 3; i++)
            {
                int Id = Convert.ToInt32(ids[(i-4) * 2]); //get searchIds
                TwitterSearchI tempSearch = nn.getSearch(Id);
               
                if (tempSearch != null)
                {
                    DateTime temp = (DateTime)(tempSearch.searchInfomation.finishedLastTwitterDate);
                    if (first)
                    {
                        dt = temp;
                        first = false;
                    }
                    else if (temp < dt)
                    {
                        dt = temp;
                    }
                }
            }
            return dt;
        }
        List<long[]> generateDateMap(DateTime start, DateTime end,ref List<long> interval)
        {
            List < long[] > finalMap = new List<long[]>();
            DateTime starti = new DateTime(start.Year,start.Month,start.Day,start.Hour,0,0);
            DateTime endi = new DateTime(end.Year,end.Month,end.Day,end.Hour,0,0);

            DateTime monthTmp = new DateTime(starti.Year, starti.Month ,1);
            DateTime monthTmpEnd = new DateTime(endi.Year, endi.Month,1);
            monthTmpEnd = monthTmpEnd.AddMonths(1);
            monthTmp = monthTmp.AddMonths(-1);
            long intervalNumber = (int)(endi - starti).TotalHours;
            long intervalPixels;
            if (intervalNumber >= 500)
                intervalPixels = intervalNumber;
            else
                intervalPixels = 500;
            interval.Add(intervalNumber);
            interval.Add(intervalPixels);
            while (monthTmp <= monthTmpEnd)
            {
                long[] maptemp = new long[3];
                maptemp[0] = monthTmp.Year ;
                maptemp[1] = monthTmp.Month;
                TimeSpan tempspan =monthTmp- starti;
                if (monthTmp < starti)
                {
                    maptemp[2] = -(int)tempspan.Duration().TotalHours;
                }
                else
                {
                    maptemp[2] = (int)tempspan.Duration().TotalHours;
                }
                finalMap.Add(maptemp);
                monthTmp =monthTmp.AddMonths(1);
            }
            return finalMap;

        }
        /*
         * based on each search's first tweet's date;
         **/
        private DateTime? getMaxDate(String[] ids, DemoI nn)
        {
            bool first = true;
            DateTime? dt = null;
            for (int i = 8;  i <= (Double)(ids.Count()) / 2 + 3; i++)
            {
                TwitterSearchI tempSearch = nn.getSearch(Convert.ToInt32(ids[(i - 4) * 2]));
                if (tempSearch != null)
                {
                    DateTime temp = (DateTime)tempSearch.searchInfomation.firstTwitterDate;
                    if (first)
                    {
                        dt = temp;
                        first = false;
                    }
                    else if (temp > dt)
                    {
                        dt = temp;
                    }
                }
            }
            return dt;
      
        }

       public  List<List<String>> loadTweetsForAnalyzeSelection(String dates, String searchLists, String laLos,int intervalCount=20)
        {
                String[] datesL = dates.Split('#');
                String[] searchListsL = searchLists.Split('#');
                String[] LaLosL = laLos.Split('#');
                List<List<String>> result = new List<List<string>>();

                DateTime startTime = new DateTime(Convert.ToInt32(datesL[0]), Convert.ToInt32(datesL[1]), Convert.ToInt32(datesL[2]), Convert.ToInt32(datesL[3]), 0, 0);
                DateTime endTime = new DateTime(Convert.ToInt32(datesL[4]), Convert.ToInt32(datesL[5]), Convert.ToInt32(datesL[6]), Convert.ToInt32(datesL[7]), 0, 0);
                List<int> searcheIds = new List<int>();
                for (int i = 0; i < searchListsL.Count(); i++)
                {
                    searcheIds.Add(Convert.ToInt32(searchListsL[i]));
                }
                double[] startLaLo = new double[2]; startLaLo[0] = Convert.ToDouble(LaLosL[0]); startLaLo[1] = Convert.ToDouble(LaLosL[1]);
                double[] endLaLo = new double[2]; endLaLo[0] = Convert.ToDouble(LaLosL[2]); endLaLo[1] = Convert.ToDouble(LaLosL[3]);
                List<DateTime> dateIntervals = divideDate(startTime, endTime);
                List<String> names = getNames(searcheIds);
                result.Add(names);
                result.AddRange(loadTweetsCountNew(dateIntervals, searcheIds, startLaLo, endLaLo));
                return result;
            
           

        }
        private  List<DateTime> divideDate(DateTime start, DateTime end, int intervalCount=20)
        {
            List<DateTime> divideDate = new List<DateTime>();
            if (intervalCount < 1)
            {
                intervalCount = 1;
            }
            TimeSpan ts = end - start;
            int interval = (Int32) Math.Floor(ts.Duration().TotalMinutes / intervalCount);
            DateTime ti = start;
            divideDate.Add(ti);
             for (int i = 0;i<intervalCount; i++)
            {
                ti = ti.AddMinutes(interval);
                divideDate.Add(ti);
            }
            return divideDate;
        }
        private List<List<String>> loadTweetsCount(List<DateTime> dates,List<int> searchId, Double[] startLaLo, Double[] endLaLo)
        {
            List<List<String>> lists = new List<List<string>>();
            for (int i = 0; i < dates.Count - 1; i++)
            {
                List<String> list = new List<String>();
                list.Add(dates[i].ToString("yyyy-MM-dd HH:mm:ss"));
                for (int j = 0; j < searchId.Count; j++)
                {
                    list.Add(dataBase.loadTweetsCount(dates[i], dates[i + 1], startLaLo, endLaLo, searchId[j]).ToString());
                }
                lists.Add(list);
            }
            return lists;
        }
        private List<List<String>> loadTweetsCountNew(List<DateTime> dates, List<int> searchId, Double[] startLaLo, Double[] endLaLo)
        {
            List<List<long>> data = new List<List<long>>();
            List<List<String>> lists = new List<List<string>>();
                for (int j = 0; j < searchId.Count; j++)
                {
                    data.Add(dataBase.loadTweetsCountNew(dates,startLaLo,endLaLo, searchId[j]));
                }
            for (int i = 0; i < dates.Count - 1; i++)
            {
                List<String> list = new List<String>();
                list.Add(dates[i].ToString("yyyy-MM-dd HH:mm:ss"));
                for(int j = 0; j < data.Count; j++)
                {
                    list.Add(data[j][i].ToString());
                }
                lists.Add(list);
            }
            return lists;
           
        }
        //return a list of search Names
        private List<String> getNames(List<int> searcheIds)
        {
           
            List<String> names = new List<string>();
            names.Add("Date");
            for (int i = 0; i < searcheIds.Count; i++) {
                TwitterSearchI tempSearch = getSearch(searcheIds[i]);
                if (tempSearch != null)
                {
                    names.Add(tempSearch.searchInfomation.searchTitle);
                }
                else
                    names.Add("deleted");
            }
            return names;
        }



        #endregion
    }


}









