using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Data;
using System.Threading;
namespace TwitterSearch
{

    public interface DBI
    {
        
            void innitial(String DB_name = "lol", String DB_pass = "", String server = "localhost", String user_name = "root");
            DateTime? getLastTwitterDate();
            TwitterModel loadBasicTwitterByRealId(long realId);
        #region Search
            int getSearchStatus(TwitterSearchI s);
        /// <summary>
        /// return the searchId in database; -1 means error;
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
            int saveNewSearch(TwitterSearchI s);
            bool updateSearch(TwitterSearchI s);
            bool deleteSearch(TwitterSearchI s);
        /// <summary>
        /// return -2 means error; -1 means not decided; 1 running; 2 stopped
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
            bool updateSearchStatus(TwitterSearchI s);
            bool saveBasicTweets(Twitters s);
            List<Searchinfo> getAllSearchInfo();
            bool updateSearchOnResume(TwitterSearchI s);// when resume and detected did not save search normally need to get lastmax tweets's info
       #endregion

       #region location
            bool updateTweets(List<TwitterModel> list);
            List<TwitterModel> loadBasicTweets(long lastMaxId, int tweetCount);//lastMaxId means get tweets after that id, tweetcount means how many tweets to get
            bool updateTweetsWithLocation(Twitters ts, int algorithmId);
            bool saveAlgorithmInfo(AlgorithmInfo info);
            bool updateAlgorithmInfo(AlgorithmInfo info);
            List<AlgorithmInfo> loadAlgorithmInfo();
            DateTime? loadLatestLocationEffectiveDate(TwitterSearchI tsi, int algorithmId = 0);//load the search's latest date which has been handeled by location with id algorithmId
        #endregion

        #region Analyze
            List<TwitterModel> loadFullTweets(TwitterSearch search, DateTime start, DateTime end);//return a list of tweets from start to end
            List<List<double>> loadTinyTweets(TwitterSearchI tsi, DateTime start, DateTime end,int colorId,int algorithmId);
            long loadTweetsCount(DateTime start, DateTime end, Double[] startLaLo, Double[] endLaLo, int searchId, int algorithmId = 0);
            List<long> loadTweetsCountNew(List<DateTime> times, Double[] startLaLo, Double[] endLaLo, int searchId, int algorithmId = 0);
       
        #endregion

    }
  
    public class BasicMysqlHelper
    {
        protected String DB_name;
        protected String DB_pass;
        protected String server;
        protected String user_name;
        protected static String mysqlcon;
        public BasicMysqlHelper(String DB_name, String DB_pass, String server, String user_name)
        {
            innitial(DB_name, DB_pass, server, user_name);
        }
        public BasicMysqlHelper()
        {
            innitial();
        }
        public void innitial(String DB_name = "test", String DB_pass = "", String server = "localhost", String user_name = "root")
        {
            this.DB_name = DB_name;
            this.DB_pass = DB_pass;
            this.server = server;
            this.user_name = user_name;
            mysqlcon = "server=" + server + ";uid=" + user_name + ";" + "pwd=" + DB_pass + ";database=" + DB_name + ";";   
        }


        protected MySqlConnection connect(int i = 0)
        {
            switch (i)
            {
                case 1:
                    Thread.Sleep(config.dataBaseConnExecptionWaitInterval[0]);
                    break;
                case 2:
                    Thread.Sleep(config.dataBaseConnExecptionWaitInterval[1]);
                    break;
                case 3:
                    Thread.Sleep(config.dataBaseConnExecptionWaitInterval[2]);
                    break;
                case 4:
                    Thread.Sleep(config.dataBaseConnExecptionWaitInterval[3]);
                    break;
            }

            try
            {
                MySqlConnection conn = new MySql.Data.MySqlClient.MySqlConnection();
                conn.ConnectionString = mysqlcon;
                conn.Open();
                return conn;
            }
            catch (MySqlException ex)
            {
                i++;
                if (config.debug)
                {
                    throw (new Exception("can't connnect" + ex));
                }
                else
                {
                    if (i >= 5)
                    {
                        throw (new Exception("can't connnect" + ex));
                    }
                   return connect(i);
                }    
            }
        }
       
    }

    public class MysqlHelper :BasicMysqlHelper, DBI
    {
        public TwitterModel loadBasicTwitterByRealId(long realId)
        {
            MySqlConnection conn = connect();
            String sentence = "select * from tweets   where realTwitterId = " + realId + " ORDER BY realTwitterId limit " + 1;
            MySqlCommand cmd = new MySqlCommand(sentence, conn);
            MySqlDataReader reader = null;


            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                reader = cmd.ExecuteReader();
               
                if (reader.Read())
                {
                    List<double> las = new List<double>();
                    List<double> los = new List<double>();
                    for (int i = 1; i <= 4; i++)
                    {
                        var ordinalla = reader.GetOrdinal((String)("PlaceLa" + i));
                        if (reader.IsDBNull(ordinalla)) // if there is no PlaceLai just break
                        {
                            break;
                        }

                        las.Add((double)reader[(String)("PlaceLa" + i)]);
                        los.Add((double)reader[(String)("PlaceLo" + i)]);
                    }
                    TwitterModel tm = new TwitterModel((long)reader["realTwitterId"], (long)reader["APITwitterId"], (int)reader["searchId"], (long)reader["userId"], (String)reader["content"], (String)reader["profileLocationContent"], las, los, (DateTime)reader["createAt"]);
                    return tm;
                }
                return null;
               
            }
            catch (Exception ex)
            {
                if (config.debug)
                {
                    throw ex;
                }else
                {
                    return null;
                }

            }
            finally
            {
                if (reader != null)
                    reader.Close();
                conn.Close();

            }
        }
       
        #region Search
        public int saveNewSearch(TwitterSearchI s)
        {

            MySqlConnection conn = connect();
            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                string cmdText = @"INSERT INTO search (searchTitle ,deleted,searchStatus,include ,exclude,startDate ,endDate ,firstTwitterDate ,tempLastTwitterDate ,tempLastTwitterId ,finishedLastTwitterDate, finishedLastTwitterId ) VALUES (@searchTitle ,@deleted,@searchStatus,@include ,@exclude,@startDate ,@endDate ,@firstTwitterDate ,@tempLastTwitterDate ,@tempLastTwitterId ,@finishedLastTwitterDate,@finishedLastTwitterId )";
                MySqlCommand command = new MySqlCommand(cmdText, conn);

                command.Parameters.AddWithValue("@searchTitle", s.searchInfomation.searchTitle);
                command.Parameters.AddWithValue("@deleted", s.searchInfomation.deleted);
                command.Parameters.AddWithValue("@searchStatus", s.searchInfomation.status);
                String tempI = null;
                String tempE = null;
                foreach (String i in s.searchInfomation.include)
                {
                    if (tempI != null)
                        tempI += '\n';
                    tempI += i;
                }
                foreach (String i in s.searchInfomation.exclude)
                {
                    if (tempE != null)
                        tempE += '\n';
                    tempE += i;
                }
                command.Parameters.AddWithValue("@include", tempI);
                command.Parameters.AddWithValue("@exclude", tempE);
                command.Parameters.AddWithValue("@startDate", s.searchInfomation.startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                if (s.searchInfomation.endDate != null)
                {
                    command.Parameters.AddWithValue("@endDate", ((DateTime)s.searchInfomation.endDate).ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else
                {
                    command.Parameters.AddWithValue("@endDate", null);
                }
                if (s.searchInfomation.firstTwitterDate != null)
                {
                    command.Parameters.AddWithValue("@firstTwitterDate", ((DateTime)s.searchInfomation.firstTwitterDate).ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else
                {
                    command.Parameters.AddWithValue("@firstTwitterDate", null);
                }
                if (s.searchInfomation.tempLastTwitterDate != null)
                {
                    command.Parameters.AddWithValue("@tempLastTwitterDate", ((DateTime)s.searchInfomation.tempLastTwitterDate).ToString("yyyy-MM-dd HH:mm:ss"));

                }
                else
                {
                    command.Parameters.AddWithValue("@tempLastTwitterDate", null);
                }
                if (s.searchInfomation.finishedLastTwitterDate != null)
                {
                    command.Parameters.AddWithValue("@finishedLastTwitterDate", ((DateTime)s.searchInfomation.finishedLastTwitterDate).ToString("yyyy-MM-dd HH:mm:ss"));

                }
                else
                {
                    command.Parameters.AddWithValue("@finishedLastTwitterDate", null);
                }
                command.Parameters.AddWithValue("@tempLastTwitterId", s.searchInfomation.tempLastTwitterId);
                command.Parameters.AddWithValue("@finishedLastTwitterId", s.searchInfomation.finishedLastTwitterId);
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                command.ExecuteNonQuery();
                int id = (int)command.LastInsertedId;
                return id;
            }
            catch (Exception e)
            {
                if (config.debug)
                {
                    throw e;
                }
                else
                {
                    return -1;
                }
            }
            finally
            {
                conn.Close();
            }
           
        }
        public bool updateSearch(TwitterSearchI s)
        {
            MySqlConnection conn = connect();
            if (conn == null)
            {
                return false;
            }
            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                string cmdText = @"update  search set searchStatus = @searchStatus, firstTwitterDate = @firstTwitterDate , finishedLastTwitterDate = @finishedLastTwitterDate , finishedLastTwitterId= @finishedLastTwitterId  , tempLastTwitterId =@tempLastTwitterId ,  tempLastTwitterDate = @tempLastTwitterDate where searchId = @searchId";
                MySqlCommand command = new MySqlCommand(cmdText, conn);
                command.Parameters.AddWithValue("@searchStatus", s.searchInfomation.status);
                if (s.searchInfomation.firstTwitterDate != null)
                {
                    command.Parameters.AddWithValue("@firstTwitterDate", ((DateTime)s.searchInfomation.firstTwitterDate).ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else
                {
                    command.Parameters.AddWithValue("@firstTwitterDate", null);
                }
                if (s.searchInfomation.finishedLastTwitterDate != null)
                {
                    command.Parameters.AddWithValue("@finishedLastTwitterDate", ((DateTime)s.searchInfomation.finishedLastTwitterDate).ToString("yyyy-MM-dd HH:mm:ss"));

                }
                else
                {
                    command.Parameters.AddWithValue("@finishedLastTwitterDate", null);
                }
                if (s.searchInfomation.tempLastTwitterDate != null)
                {
                    command.Parameters.AddWithValue("@tempLastTwitterDate", ((DateTime)s.searchInfomation.tempLastTwitterDate).ToString("yyyy-MM-dd HH:mm:ss"));

                }
                else
                {
                    command.Parameters.AddWithValue("@tempLastTwitterDate", null);
                }
                command.Parameters.AddWithValue("@tempLastTwitterId", s.searchInfomation.tempLastTwitterId);
                command.Parameters.AddWithValue("@finishedLastTwitterId", s.searchInfomation.finishedLastTwitterId);
                command.Parameters.AddWithValue("@searchId", s.searchInfomation.searchId);             
                command.ExecuteNonQuery();
                command.Parameters.Clear();
                return true;
            }
            catch(Exception e)
            {
                if (config.debug)
                {
                    throw e;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                conn.Close();
            }
            
        }
        public bool deleteSearch(TwitterSearchI s)
        {
            MySqlConnection conn = connect();
            if (conn == null)
            {
                return false;
            }
            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                string cmdText = @"update search set deleted = 1 where searchId = " +s.searchInfomation.searchId;
                MySqlCommand command = new MySqlCommand(cmdText, conn);
                int i =command.ExecuteNonQuery();
                if (i == 1)
                {
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                if (config.debug)
                {
                    throw e;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                conn.Close();
            }
        }
        public bool updateSearchStatus(TwitterSearchI s)
        {
            MySqlConnection conn = connect();
            if (conn == null)
            {
                return false;
            }
            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                string cmdText = @"update search set searchStatus = " + s.searchInfomation.status + " where searchId = " + s.searchInfomation.searchId;
                MySqlCommand command = new MySqlCommand(cmdText, conn);
                int i = command.ExecuteNonQuery();
                if (i == 1)
                {
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                if (config.debug)
                {
                    throw e;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                conn.Close();
            }
        }
        public bool saveBasicTweets(Twitters s)
        {
            MySqlConnection conn = connect();
            if (conn.State == ConnectionState.Closed)
                conn.Open();
            string cmdText = @"insert into  tweets (APITwitterId ,searchId,userScreenName ,userId,content,createAt,profileLocationContent ,PlaceLa1 ,PlaceLo1 ,PlaceLa2 ,PlaceLo2 ,PlaceLa3 ,PlaceLo3 ,PlaceLa4 ,PlaceLo4 ) values (@APITwitterId ,@searchId,@userScreenName ,@userId,@content,@createAt,@profileLocationContent ,@PlaceLa1 ,@PlaceLo1 ,@PlaceLa2 ,@PlaceLo2 ,@PlaceLa3 ,@PlaceLo3 ,@PlaceLa4 ,@PlaceLo4 )";
            try
            {
                foreach (TwitterModel tm in s.allTweets)
                {
                    MySqlCommand command = new MySqlCommand(cmdText, conn);
                    command.Parameters.AddWithValue("@APITwitterId", tm.APITwitterId);
                    command.Parameters.AddWithValue("@searchId", tm.searchId);
                    command.Parameters.AddWithValue("@userScreenName", tm.userScreenName);
                    command.Parameters.AddWithValue("@userId", tm.userId);
                    command.Parameters.AddWithValue("@content", tm.content);
                    command.Parameters.AddWithValue("@createAt", tm.createAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("@profileLocationContent", tm.profileLocationContent);
                    switch (tm.PlaceLa.Count)
                    {
                        case 0:
                            command.Parameters.AddWithValue("@PlaceLa1", null);
                            command.Parameters.AddWithValue("@PlaceLo1", null);
                            command.Parameters.AddWithValue("@PlaceLa2", null);
                            command.Parameters.AddWithValue("@PlaceLo2", null);
                            command.Parameters.AddWithValue("@PlaceLa3", null);
                            command.Parameters.AddWithValue("@PlaceLo3", null);
                            command.Parameters.AddWithValue("@PlaceLa4", null);
                            command.Parameters.AddWithValue("@PlaceLo4", null);
                            break;
                        case 1:
                            command.Parameters.AddWithValue("@PlaceLa1", tm.PlaceLa[0]);
                            command.Parameters.AddWithValue("@PlaceLo1", tm.PlaceLo[0]);
                            command.Parameters.AddWithValue("@PlaceLa2", null);
                            command.Parameters.AddWithValue("@PlaceLo2", null);
                            command.Parameters.AddWithValue("@PlaceLa3", null);
                            command.Parameters.AddWithValue("@PlaceLo3", null);
                            command.Parameters.AddWithValue("@PlaceLa4", null);
                            command.Parameters.AddWithValue("@PlaceLo4", null);
                            break;
                        case 2:
                            command.Parameters.AddWithValue("@PlaceLa1", tm.PlaceLa[0]);
                            command.Parameters.AddWithValue("@PlaceLo1", tm.PlaceLo[0]);
                            command.Parameters.AddWithValue("@PlaceLa2", tm.PlaceLa[1]);
                            command.Parameters.AddWithValue("@PlaceLo2", tm.PlaceLo[1]);
                            command.Parameters.AddWithValue("@PlaceLa3", null);
                            command.Parameters.AddWithValue("@PlaceLo3", null);
                            command.Parameters.AddWithValue("@PlaceLa4", null);
                            command.Parameters.AddWithValue("@PlaceLo4", null);

                            break;
                        case 3:
                            command.Parameters.AddWithValue("@PlaceLa1", tm.PlaceLa[0]);
                            command.Parameters.AddWithValue("@PlaceLo1", tm.PlaceLo[0]);
                            command.Parameters.AddWithValue("@PlaceLa2", tm.PlaceLa[1]);
                            command.Parameters.AddWithValue("@PlaceLo2", tm.PlaceLo[1]);
                            command.Parameters.AddWithValue("@PlaceLa3", tm.PlaceLa[2]);
                            command.Parameters.AddWithValue("@PlaceLo3", tm.PlaceLo[2]);
                            command.Parameters.AddWithValue("@PlaceLa4", null);
                            command.Parameters.AddWithValue("@PlaceLo4", null);

                            break;
                        case 4:
                            command.Parameters.AddWithValue("@PlaceLa1", tm.PlaceLa[0]);
                            command.Parameters.AddWithValue("@PlaceLo1", tm.PlaceLo[0]);
                            command.Parameters.AddWithValue("@PlaceLa2", tm.PlaceLa[1]);
                            command.Parameters.AddWithValue("@PlaceLo2", tm.PlaceLo[1]);
                            command.Parameters.AddWithValue("@PlaceLa3", tm.PlaceLa[2]);
                            command.Parameters.AddWithValue("@PlaceLo3", tm.PlaceLo[2]);
                            command.Parameters.AddWithValue("@PlaceLa4", tm.PlaceLa[3]);
                            command.Parameters.AddWithValue("@PlaceLo4", tm.PlaceLo[3]);
                            break;

                    }

                    command.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception e)
            {
                if (config.debug)
                {
                    throw e;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                conn.Close(); 
            }

        }
        
        public int getSearchStatus(TwitterSearchI s)
        {
            MySqlConnection conn = connect();
            MySqlCommand cmd = new MySqlCommand("select searchStatus from search where searchId = " + s.searchInfomation.searchId, conn);
            MySqlDataReader reader = null;
            try
            {
               
                if (conn.State == ConnectionState.Closed)
                    conn.Open();     
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    return Int32.Parse (reader[0].ToString());
                }
                return -1;

            }
            catch (Exception ex) {
                if (config.debug)
                {
                    throw ex;
                }
                else
                {
                    return -2;
                }
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                conn.Close();

            }
        }

        public List<Searchinfo> getAllSearchInfo()
        {
            MySqlConnection conn = connect();
            MySqlCommand cmd = new MySqlCommand("select * from search where deleted =  " + 0 , conn);
            MySqlDataReader reader = null;


            try
            {

                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                reader = cmd.ExecuteReader();
                List<Searchinfo> temp = new List<Searchinfo>();
                        
                while (reader.Read())
                {
                    String inc = reader["include"] as String;
                    String exc = reader["exclude"] as String;

                    List<String> includeL = new List<string>();
                    List<String> excludeL = new List<string>();
                    if (inc != null)
                    includeL = inc.Split('\n').ToList();
                    if(exc!=null)
                    excludeL = exc.Split('\n').ToList();
                    Searchinfo dd =  new Searchinfo((int)reader["searchId"],(String)reader["searchTitle"],(bool) reader["deleted"],(int)reader["searchStatus"],includeL,excludeL,(DateTime) (reader["startDate"] as DateTime?),  reader["endDate"] as DateTime?, reader["firstTwitterDate"] as DateTime?, reader["tempLastTwitterDate"] as DateTime?,(long)reader["tempLastTwitterId"], reader["finishedLastTwitterDate"] as DateTime?, (long)reader["finishedLastTwitterId"]);
                    temp.Add(dd);
                }
                return temp;
               

            }
            catch (Exception ex)
            {
                if (config.debug)
                {
                    throw ex;
                }
                else
                {
                    throw ex;
                }
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                conn.Close();

            }
        }
        
        public bool updateSearchOnResume(TwitterSearchI s)// when resume and detected did not save search normally need to get information
        {
            MySqlConnection conn = connect();
            MySqlCommand cmd = new MySqlCommand("select * from tweets     where searchId =" + s.searchInfomation.searchId + " ORDER BY realTwitterId DESC limit 1", conn);
            MySqlDataReader reader = null;
            s.searchInfomation.needResume = true;

            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                   if(s.searchInfomation.finishedLastTwitterId < (long)reader["realTwitterId"]) // means new twitter is stroed in database in this running search
                    {
                        s.searchInfomation.maxIdOnResume = (long)reader["APITwitterId"];                                                         
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                if (config.debug)
                {
                    throw ex;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                conn.Close();

            }
        }
        #endregion
        #region location
        public bool updateTweets(List<TwitterModel> list)//xxxxx
        {
            return true;
           
        }
        public List<TwitterModel> loadBasicTweets(long lastMaxId, int tweetCount)//load tweets but ignore location from location methods, lastMaxId means get tweets after that id, tweetcount means how many tweets to get
        {
            MySqlConnection conn = connect();
            String sentence = "select * from tweets   where realTwitterId >= " + lastMaxId + " ORDER BY realTwitterId limit " + tweetCount;
            MySqlCommand cmd = new MySqlCommand(sentence, conn);
            MySqlDataReader reader = null;


            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                reader = cmd.ExecuteReader();
                List<TwitterModel> twitters = new List<TwitterModel>();
                while (reader.Read())
                {
                    List<double> las = new List<double>();
                    List<double> los = new List<double>();
                    for (int i = 1; i <= 4; i++)
                    {
                        var ordinalla = reader.GetOrdinal((String)("PlaceLa" + i));
                        if (reader.IsDBNull(ordinalla)) // if there is no PlaceLai just break
                        {
                            break;
                        }
                        
                        las.Add((double)reader[(String)("PlaceLa" + i)]);
                        los.Add((double)reader[(String)("PlaceLo" + i)]);
                    }
                    TwitterModel tm = new TwitterModel((long) reader["realTwitterId"], (long) reader["APITwitterId"], (int) reader["searchId"], (long)reader["userId"], (String)reader["content"], (String)  reader["profileLocationContent"],  las, los,( DateTime)  reader["createAt"]);
                    twitters.Add(tm);
                }

                return twitters;
            }
            catch (Exception ex)
            {
                if (config.debug)
                {
                    throw ex;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                conn.Close();

            }
        }
        public bool updateTweetsWithLocation(Twitters ts, int algorithmId)//save location of tweet 
        {
            MySqlConnection conn = connect();
            try
            {
                if (ts.allTweets.Count < 1)
                {
                    return true;
                }
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                int algorithmNumber = algorithmId;
                string locationString = "La" + algorithmNumber + " = @la, Lo" + algorithmNumber + " = @lo, city" + algorithmNumber + " = @city, state" + algorithmNumber + " = @state, country" + algorithmNumber+" = @country";
                string cmdText = @"update  tweets set "+ locationString+ " where realTwitterId = @id";
                MySqlCommand command = new MySqlCommand(cmdText, conn);


                foreach (TwitterModel tm in ts.allTweets)
                {
                    command.Parameters.Clear();
                    if (tm.location.Count > 0 )
                    {
                        command.Parameters.AddWithValue("@la", tm.location[0].la);
                        command.Parameters.AddWithValue("@lo", tm.location[0].lo);
                        command.Parameters.AddWithValue("@city", tm.location[0].city);
                        command.Parameters.AddWithValue("@state", tm.location[0].state);
                        command.Parameters.AddWithValue("@country", tm.location[0].country);
                        command.Parameters.AddWithValue("@id", tm.realTwitterId);

                        if (command.ExecuteNonQuery() <= 0)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                if (config.debug)
                {
                    throw ex;
                }
                else
                {
                    return false;
                }
            }
            finally
            {

                conn.Close();

            }
        }
        // List<TwitterModel> getTweets(TwitterSearch search, long startTweetId, int TweetNumber)//will be used when adding a new Algorithm //xxxxxxxxx

        public bool saveAlgorithmInfo(AlgorithmInfo info)//to do
        {
            throw (new Exception("to do"));
        }
        public bool updateAlgorithmInfo(AlgorithmInfo info)
        {
            MySqlConnection conn = connect();
            String tempdate = "null";
            if (info.lastDate !=null)
            tempdate =((DateTime) (info.lastDate)).ToString("yyyy-MM-dd HH:mm:ss");
            String temp = "update algorithm set lastMaxId = " + info.lastMaxId + " , lastDate = @tempdate"  + "  where algorithmId = " + info.algorithmId;

            MySqlCommand cmd = new MySqlCommand(temp, conn);
            cmd.Parameters.AddWithValue("@tempdate",tempdate);


            try
            {

                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                if (cmd.ExecuteNonQuery() <= 0)
                {
                    return false;
                }
                return true;
              
               

            }
            catch (Exception ex)
            {
                if (config.debug)
                {
                    throw ex;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                
                conn.Close();

            }
        }
        public List<AlgorithmInfo> loadAlgorithmInfo()
        {
            MySqlConnection conn = connect();
            MySqlCommand cmd = new MySqlCommand("select * from algorithm " , conn);
            MySqlDataReader reader = null;


            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                reader = cmd.ExecuteReader();
                List<AlgorithmInfo> als = new List<AlgorithmInfo>();
                while (reader.Read())
                {
                    AlgorithmInfo af = new AlgorithmInfo((long) reader["lastMaxId"], (String) reader["algorithmName"], reader["lastDate"] as DateTime?, (int) reader["algorithmId"]);
                    als.Add(af);
                }

                return als;
            }
            catch (Exception ex)
            {
                if (config.debug)
                {
                    throw ex;
                }
                else
                {
                    throw ex;
                }
            }
            finally
            {
                if(reader!=null)
                reader.Close();
                conn.Close();

            }
        }
        public AlgorithmInfo loadAlgorithmInfo(int algorithmId)
        {
            MySqlConnection conn = connect();
            MySqlCommand cmd = new MySqlCommand("select * from algorithm where algorithmId = " + algorithmId, conn);
            MySqlDataReader reader = null;

            AlgorithmInfo af = null;
            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    af = new AlgorithmInfo((long)reader["lastMaxId"], (String)reader["algorithmName"], reader["lastDate"] as DateTime?, (int)reader["algorithmId"]);
                }

                return af;
            }
            catch (Exception ex)
            {
                if (config.debug)
                {
                    throw ex;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                conn.Close();

            }
        }
        public DateTime? getLastTwitterDate()
        {
            MySqlConnection conn = connect();
            MySqlCommand cmd = new MySqlCommand("select createAt from tweets ORDER BY realTwitterId DESC limit 1", conn);
            MySqlDataReader reader = null;


            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                reader = cmd.ExecuteReader();
                
                if (reader.Read())
                {
                    DateTime? h = reader["createAt"] as DateTime?;
                    return  h;

                }

                return null;
            }
            catch (Exception ex)
            {
                if (config.debug)
                {
                    throw ex;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                conn.Close();

            }
        }

        public DateTime? loadLatestLocationEffectiveDate(TwitterSearchI tsi, int algorithmId = 0)
        {
            AlgorithmInfo al = loadAlgorithmInfo(algorithmId);
            TwitterModel tm;
            if (al == null||al.lastMaxId<0)
            {
                return null;
            }
            else
            {
                tm = loadBasicTwitterByRealId(al.lastMaxId);
                if (tm == null)
                {
                    return null;
                }
                if (tsi.searchInfomation.finishedLastTwitterDate==null)
                {
                    return null;
                }
                if (tsi.searchInfomation.finishedLastTwitterDate<=tm.createAt)
                {
                    return tm.createAt;
                }
            }
            MySqlConnection conn = connect();
            MySqlCommand cmd = new MySqlCommand("select createAt from tweets where searchId = "+  tsi.searchInfomation.searchId+" and realTwitterId < " + al.lastMaxId + " and APITwitterId < " + tm.APITwitterId +" ORDER BY APITwitterId DESC limit 1", conn);
            MySqlDataReader reader = null;


            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    DateTime? h = reader["createAt"] as DateTime?;
                    return h;

                }

                return null;
            }
            catch (Exception ex)
            {
                if (config.debug)
                {
                    throw ex;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                conn.Close();

            }
        }
        #endregion
        #region Analyze
        public List<TwitterModel> loadFullTweets(TwitterSearch search, DateTime start, DateTime end)//return a list of tweets from start to end
        {
            //xxxxxxxxxxxx
            throw (new Exception("to do"));
        }
       
        public List<List<double>> loadTinyTweets(TwitterSearchI tsi, DateTime start, DateTime end,int colorId,int algorithmId = 0)
        {
            MySqlConnection conn = connect();
            String sentence = "select * from tweets where La"+ algorithmId + " is not null and  @start <= createAt   and createAt <= @end and searchId = "+tsi.searchInfomation.searchId+ " ORDER BY rand() limit " + config.analyzeTwitterLoaderCount;
            MySqlCommand cmd = new MySqlCommand(sentence, conn);
            cmd.Parameters.AddWithValue("@start",start.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd HH:mm:ss"));

            MySqlDataReader reader = null;
            Random rd = new Random();

            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                reader = cmd.ExecuteReader();
                List<List<double>> tts = new List<List<double>>();
                while (reader.Read())
                {

                    List<double> tt = new List<double>();
                   
                    tt.Add((double)reader["La" + algorithmId] + ((double)rd.Next(0, 100)- 50) / 50);
                    tt.Add((double)reader["Lo" + algorithmId] + ((double)rd.Next(0, 100)- 50) / 50);
                    tt.Add(colorId);
                    tts.Add(tt);
                }

                return tts;
            }
            catch (Exception ex)
            {
                if (config.debug)
                {
                    throw ex;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                conn.Close();

            }
        }
        public long loadTweetsCount(DateTime start, DateTime end, double[] startLaLo, double[] endLaLo, int searchId, int algorithmId = 0)
        {
            MySqlConnection conn = connect();
            String La = "La" + algorithmId;
            String Lo = "Lo" + algorithmId;
            double minLa = endLaLo[0], maxLa = startLaLo[0];
            double minLo = endLaLo[1], maxLo = startLaLo[1];
            if(startLaLo[0]< endLaLo[0])
            {
                minLa = startLaLo[0];
                maxLa = endLaLo[0];
            }
            if (startLaLo[1] < endLaLo[1])
            {
                minLo = startLaLo[1];
                maxLo = endLaLo[1];
            }

            String ss = " and " + La + " < " + maxLa + " and " + Lo + " < " + maxLo;
            String ss1 = " and " + minLa + " < " + La + " and " + minLo + " < " + Lo;
            String sentence = "select count(*) from tweets where searchId = " + searchId +" and " + La + " is not null " + ss + ss1 +" and  @start <= createAt   and createAt <= @end ";
            MySqlCommand cmd = new MySqlCommand(sentence, conn);
            cmd.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd HH:mm:ss"));


            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();

                return Convert.ToInt64( cmd.ExecuteScalar()) ;
            }
            catch (Exception ex)
            {
                if (config.debug)
                {
                    throw ex;
                }
                else
                {
                    return -1;
                }
            }
            finally
            {
               
                conn.Close();

            }
        }
        public List<long> loadTweetsCountNew(List<DateTime> times, Double[] startLaLo, Double[] endLaLo, int searchId, int algorithmId = 0)
        {

            MySqlDataReader reader = null;
            MySqlConnection conn = connect();
            String La = "La" + algorithmId;
            String Lo = "Lo" + algorithmId;
            double minLa = endLaLo[0], maxLa = startLaLo[0];
            double minLo = endLaLo[1], maxLo = startLaLo[1];
            if (startLaLo[0] < endLaLo[0])
            {
                minLa = startLaLo[0];
                maxLa = endLaLo[0];
            }
            if (startLaLo[1] < endLaLo[1])
            {
                minLo = startLaLo[1];
                maxLo = endLaLo[1];
            }
            long[] arr;
            arr = new long[times.Count - 1];

            String sentence = "select "+La+", "+ Lo+", APITwitterId, createAt"+  " from tweets where " + " searchId = " + searchId + " and "+La + " is not null and " + Lo + " is not null";
            MySqlCommand cmd = new MySqlCommand(sentence, conn);

            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                reader = cmd.ExecuteReader();
                SmallTweets st = new SmallTweets();
                List<SmallTweet> st2 = new List<SmallTweet>();
                while (reader.Read())
                {
                    List<double> tt = new List<double>();
                    SmallTweet stt = new SmallTweet((double)reader["La" + algorithmId], (double)reader["Lo" + algorithmId], (DateTime)reader["createAt"], (long)reader["APITwitterId"]);
                    st.push(stt);
                    st2.Add(stt);

                }

                st.move();//desc order
                SmallTweetNode temps;
                long hello;
                
                int index = arr.Length - 1;
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = 0;
                }
                if (st.Count != 0)
                {
                    temps = st.smallStart;
                    hello = temps.value.ApiTwitterId;
                }
                else
                {
                    return arr.ToList();
                }
                while (temps.hasNext)
                {
                    if (temps.next.value.ApiTwitterId > hello)
                    {
                        Console.WriteLine("error" + temps.value.ApiTwitterId + "  " + temps.next.value.ApiTwitterId);
                    }
                    temps = temps.next;
                    hello = temps.value.ApiTwitterId;
                }

                SmallTweetNode temp = st.smallStart;
                DateTime oldestDate = times.Last();
                for (; temp.hasNext;)
                {
                    if (temp.value.createAt <= oldestDate)
                    {
                        break;
                    }
                    temp = temp.next;
                }
                for (; temp.hasNext;)
                {
                    if (temp.value.createAt >= times[index])
                    {
                        if (checkedLaLo(temp.value, minLa, minLo, maxLa, maxLo))
                            arr[index]++;
                    }
                    else
                    {
                        index--;
                        if (index == -1)
                        {
                            return arr.ToList();
                        }
                        while (temp.value.createAt < times[index])
                        {
                            index--;
                            if (index == -1)
                            {
                                return arr.ToList();
                            }
                        }
                        if (checkedLaLo(temp.value, minLa, minLo, maxLa, maxLo))
                            arr[index]++;
                    }
                    temp = temp.next;
                }
                return arr.ToList();
            }
            catch (Exception e)
            {
                if (config.debug)
                {
                    System.Diagnostics.Debug.WriteLine("error " + e.Message);
                    throw (e);
                }
                else
                {
                    return arr.ToList();
                }
               
            }

            finally
            {
                if (reader != null)
                    reader.Close();
                conn.Close();

            }

        }
        private bool checkedLaLo(SmallTweet st,double minLa, double minLo,double maxLa, double maxLo)
        {
            if(minLa<=st.la&& st.la <= maxLa && minLo <= st.lo && maxLo >= st.lo)
            {
                return true;
            }
            return false;
        }
        #endregion
    }
}







