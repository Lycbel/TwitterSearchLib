using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using TwitterSearch;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Configuration;
using MySql.Data;
using System.Data;
using System.Collections;
using TwitterSearch;
namespace ConsoleApplication2
{
    public class BasicMysqlHelper
    {
        protected String DB_name;
        protected String DB_pass;
        protected String server;
        protected String user_name;
        protected static String mysqlcon;

        public MySqlConnection conn;
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
            if (!connect())
            {
                Console.WriteLine("Database Connection Error");
            }
        }
        public bool connect()
        {
            try
            {
                conn = new MySql.Data.MySqlClient.MySqlConnection();
                conn.ConnectionString = mysqlcon;
                conn.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                throw ex;
            }

        }

        public void close()
        {
            conn.Close();
        }
    }
        class Program
    {
        static void Main(string[] args)
        {

            string m = null;
            m.ToCharArray();





            /*
            TimeSpan ss = TimeSpan.FromHours(1);
           DateTime tim1 = new DateTime(2017,12,11,12,30,10);
            List<DateTime> andy = new List<DateTime>();

            andy.Add(tim1);
           tim1 = tim1.AddMonths(1);
            andy.Add(tim1);
            DateTime tim3=  tim1.AddMonths(-1);
           DateTime tim2 = new DateTime(2017, 11, 11, 12, 31, 10);
            TimeSpan tms = (TimeSpan)(tim2 - tim1);
            int hhhh = (int)tms.Duration().TotalHours;
           int y1 = tim1.Year;
            int d1 = tim1.Month;
            int d11 = tim1.Day;
            int h1 = tim1.Hour;
            List<int> l1 = new List<int>();
            List<int> l2 = new List<int>();
            List<int> l3 = new List<int>();
            List<int> l4 = new List<int>();
            List<List<int>> ll1 = new List<List<int>>();
            List<List<int>> ll2 = new List<List<int>>();
            l1.Add(1);
            l1.Add(2);
            l1.Add(3);
            l2.Add(1);
            l2.Add(2);
            l2.Add(3);
            l3.Add(1);
            l3.Add(2);
            l3.Add(3);
            l4.Add(1);
            l4.Add(5);
            l4.Add(6);
            ll1.Add(l1);
            ll1.Add(l2);
            
            ll2.Add(l3);
            ll2.Add(l4);
            ll1.AddRange(ll2);
            IEnumerable<List<int>> mm = new List<List<int>>();
               mm = mm.Concat(ll2);
            mm = mm.Concat(ll1);
            List<List<int>> mm2 = mm.ToArray().ToList();
            int[] l5= l3.ToArray();
            int a = 2;
            int b = 5;
            bool m= (a == (double)b /2);
            bool n = (3 > b / 2);
            */

            DemoI dd = new Demo();
            dd.loadTweetsForAnalyzeSelection("2017#7#18#4#2017#7#27#21", "10", "42.48035267551174#-81.02963438256657#38.259320910316205#-72.09280849658813");
            
            
          
          /*
              String in1 = "drug\ntrump\nbest";
            String   ex1 = "hello\npresident";
            for (int i = 0; i < 5; i++)
            {
                dmi.addSearch(dmi.createSearch(in1, ex1, i.ToString()));
            }
             
        
            
           /*
              String ti1 = "1";
              String in1 = "drug\nhello";
              String ex1 = "";
              dmi.addSearch(dmi.createSearch(in1, ex1, ti1));
            dmi.addSearch(dmi.createSearch(in1, ex1, ti1));
            dmi.addSearch(dmi.createSearch(in1, ex1, ti1));

            /*
              String ti2 = "2";
              String in2 = "drug\ntrump";
              String ex2 = "hello\npresident";
              dmi.addSearch(dmi.createSearch(in2, ex2, ti2));
              String ti3 = "3";
              String in3 = "health\nstudent\ndrug";
              String ex3 = "world";
              dmi.addSearch(dmi.createSearch(in3, ex3, ti3));
              */
            // dmi.deleteSearchById(1);


            
            Thread.Sleep(10000000);
        }
    }
}
