using System;
using System.Linq;
using System.Threading;
using Google.Maps.Geocoding;
using Google.Maps;
using TwitterSearch;
namespace TwitterSearchLib.locationAlgorithm
{
  
    class GoogleMapHelper : LocationHelper
    {
        
      public GoogleMapHelper()
        {
            GoogleSigned.AssignAllServices(new GoogleSigned("AIzaSyAArrBejso__Us2YOR8l23TfAAbhWkw3ps")); 
        }
        public  TwitterModel run(TwittersI tsList, int algorithmId)
        {
            
            TwitterModel tmp = null;
            int index = 0;
            for (;index< tsList.allTweets.Count;index++)
            {
                TwitterModel tm = tsList.allTweets[index];
                if (index >= config.googleLocationTweetNumberEachCycleTest)//for test
                {
                    return tsList.allTweets.Last();
                }
                int i = 0;
                Result[] rs = search(tm,tm.profileLocationContent,ref i);
                if ( rs!= null && rs.Length != 0)
                {           
                    LocationModel lm = new LocationModel(algorithmId,  rs[0].Geometry.Location.Longitude, rs[0].Geometry.Location.Latitude, null, null, null);
                    tm.addLocation(lm); 
                }
                if(i == 3) // if reach limit
                {
                    index--;
                    System.Diagnostics.Debug.WriteLine("google waiting");
                    Thread.Sleep(config.googlelocationWaitIntervalWhenLimit);
                }
              
              
                tmp = tm; 
            }
            return tmp;
           
        }
      public Result[] search(TwitterModel tm,string input,ref int i)
        {
            if(input == null)
            {
                i = -2; return null;
            }
            char[] inputs = input.ToCharArray();
            bool all = true;
            foreach (char c in inputs)
            {
                if(c!=' ')
                {
                    all = false;
                }
            }
            if (all) { i = -2; return null; }
            var request = new GeocodingRequest();
            request.Address = input;
            request.Sensor = false;
            try
            {
                var response = new GeocodingService().GetResponse(request);
                switch (response.Status)
                {
                    case ServiceResponseStatus.Ok:
                        i = -1;     
                        return response.Results;
                    case ServiceResponseStatus.OverQueryLimit:
                        i = 3;
                        return null;
                    case ServiceResponseStatus.ZeroResults:
                        i = 2;
                        break;
                    case ServiceResponseStatus.RequestDenied:
                        if (i == 4) // if denied will try again
                        {
                            return null;
                        }else
                        {
                            i = 4;
                            return  search(tm,input, ref i);
                        }
                    case ServiceResponseStatus.Unknown:
                        i = 0;
                        break;

                }
                return null;

                
            }
            catch (Exception ex)//xxxxxx
            {
                if (i == 5)
                {
                    return null;
                }else
                {
                    i = 5;
                    System.Diagnostics.Debug.WriteLine("error"+input+"--" + ex.Message);
                    Thread.Sleep(config.googlelocationWaitIntervalWhenExecption);
                    return search(tm,input, ref i);
                }
              
            }
          
        }

    }
}

