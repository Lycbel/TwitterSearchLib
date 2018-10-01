using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi.Models;
namespace TwitterSearch
{
    public interface TwittersI
    {
        List<TwitterModel> allTweets { get; set; }
        void saveBasicTweets();
        void saveLocationInTweets(int algorithmId);
        List<TwitterModel> getBasicTweets(long lastMaxId, int count);
        List<TwitterModel> getFullTweets(TwitterSearch search, DateTime start, DateTime end);
    }

    public class Twitters : TwittersI
    {
        private DBI database;
        private List<TwitterModel> allTweetsR = new List<TwitterModel>();
        public List<TwitterModel> allTweets
        {
            get
            {
                return allTweetsR;
            }
            set
            {
                allTweetsR = value;
            }
        }

        public Twitters(TwitterSearchI tsi, ISearchResult ist, DBI database)//in search convert from Itweet to twitterModel
        {
            this.database = new MysqlHelper();

            foreach (ITweet it in ist.Tweets)
            {
                TwitterModel tm = new TwitterModel(it, tsi.searchInfomation.searchId);
                this.allTweets.Add(tm);
            }

        }
        public Twitters()
        {
            this.database = new MysqlHelper();
        }

        public void saveBasicTweets() // save all tweets in allTweets to database
        {
            database.saveBasicTweets(this);
        }

        public void saveLocationInTweets(int algorithmId)
        {
            database.updateTweetsWithLocation(this, algorithmId);
        }

        public List<TwitterModel> getBasicTweets(long lastMaxId, int count)
        {
            return database.loadBasicTweets(lastMaxId, count);
        }

        public List<TwitterModel> getFullTweets(TwitterSearch search, DateTime start, DateTime end)
        {
            return database.loadFullTweets(search, start, end);
        }

    }

    public class TwitterModel
    {
        public long APITwitterId;
        public long realTwitterId; // realtwitter id will from 0 , it will from database
        public int searchId;
        public long userId;
        public String userScreenName;
        public String content;
        public DateTime createAt;
        public String profileLocationContent;
        public List<double> PlaceLa = new List<double>();//if exists might be box's points
        public List<double> PlaceLo = new List<double>();
        public List<LocationModel> location = new List<LocationModel>();
        public TwitterModel(ITweet it, int searchId) // convert from Itweet 
        {
            this.APITwitterId = it.Id;
            this.searchId = searchId;
            this.userId = it.CreatedBy.Id;
            this.userScreenName = it.CreatedBy.ScreenName;
            this.content = it.FullText;
            this.createAt = it.CreatedAt;
            this.profileLocationContent = it.CreatedBy.Location;

            if (it.Place != null && it.Place.BoundingBox != null)
            {
                foreach (ICoordinates ig in it.Place.BoundingBox.Coordinates)
                {
                    this.PlaceLa.Add(ig.Latitude);
                    this.PlaceLo.Add(ig.Longitude);
                }
            }
        }
        //for location method basic tweet infomation for location
        public TwitterModel(long realTwitterId, long APITwitterId, int searchId, long userId, String content, String profileLocationContent, List<double> PlaceLa, List<double> PlaceLo, DateTime createAt)//for location method
        {
            this.realTwitterId = realTwitterId;
            this.APITwitterId = APITwitterId;
            this.searchId = searchId;
            this.userId = userId;
            this.createAt = createAt;
            this.content = content;
            this.profileLocationContent = profileLocationContent;
            this.PlaceLa = PlaceLa;
            this.PlaceLo = PlaceLo;
        }
        public void addLocation(LocationModel lm)
        {
            this.location.Add(lm);
        }
    }
    public class SmallTweets
    {
        SmallTweetNode start;
        SmallTweetNode end;
        int count = 0;
        public int Count
        {
            get { return count; }
            set { count = value; }
        }
        public SmallTweetNode smallStart
        {
            get { return start; }
            set { start = value; }
        }
        public SmallTweetNode smallEnd
        {
            get { return end; }
            set { end = value; }
        }
        public void push(SmallTweet value)
        {
            SmallTweetNode node = new SmallTweetNode(value);
            if (count == 0)
            {
                start = node;
                end = node;
            }
            else
            {
                end.next = node;
                end = node;
            }

            count++;
        }
        public SmallTweetNode getAt(int i)
        {
           
            if (count < i + 1 || i < 0)
            {
                return null;
            }
            SmallTweetNode temp = start;

            for (int j = 0; j < i; j++)
            {
                temp = temp.next;
            }
            return temp;
        }
        public void move()
        {
            if (count == 0)
            {
                return;
            }
            SmallTweetNode temp1 = start;
            SmallTweetNode tstart1 = start;
            SmallTweetNode tend1 = null;
            SmallTweetNode tstart2 = null;
            SmallTweetNode tend2 = null;
            while (temp1.hasNext)
            {
                SmallTweetNode temp2 = temp1.next;
                if (temp2.value.ApiTwitterId > temp1.value.ApiTwitterId)//if reach the interval end
                {
                    if (tstart2 == null)//first interval complete
                    {
                        tend1 = temp1;
                        tstart2 = temp2;
                        tend1.next = null;
                    }
                    else  
                    {
                        tend2 = temp1;
                        tend2.next = tstart1;
                        tstart1 = tstart2;
                        tstart2 = temp2;
                    }

                }
               
                    temp1 = temp2;
               
            }
            if (tstart2 == null)
            {
                return;
            }else
            {
                tend2 = temp1;
                tend2.next = tstart1;
                tstart1 = tstart2;
              
            }

            start = tstart1;
            end = tend1;
        }



    }
    public class SmallTweetNode
    {
        public SmallTweet value = null;
        public SmallTweetNode next = null;
        public SmallTweetNode(SmallTweet value)
        {
            this.value = value;
        }
        public bool hasNext
        {
            get
            {
                if (next == null)
                {
                    return false;
                }
                return true;
            }
        }
    }
        public class SmallTweet
        {
            public DateTime createAt;
            public double la = -1;
            public double lo = -1;
            public long ApiTwitterId = -1;
            public SmallTweet(double la, double lo, DateTime date, long ApiTwitterId)
            {
                this.ApiTwitterId = ApiTwitterId;
                this.createAt = date;
                this.la = la;
                this.lo = lo;
            }

        }
    
        public class LocationModel
        {
            public int algorithm;
            public double? lo = null;
            public double? la = null;
            public String city = null;
            public String state = null;
            public String country = null;
            public LocationModel(int al, double lo, double la, String city, String state, String country)
            {
                this.algorithm = al;
                this.lo = lo;
                this.la = la;
                this.city = city;
                this.state = state;
                this.country = country;
            }

        }
    
}