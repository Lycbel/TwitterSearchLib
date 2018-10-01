using System;
using System.Collections.Generic;
using System.Web.Mvc;
using TwitterSearch;
namespace SearchWeb.Controllers
{

    public class SearchController : Controller
    {
        // GET: Search
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult NewSearch()
        {
            var mm = System.Web.HttpContext.Current.Application["demo"];
            DemoI nn = (DemoI)mm;
            if (Request.Form["back"] != null)
            {

                String title = Request.Unvalidated.Form["searchTitle"];
                String include = Request.Unvalidated.Form["include"];
                String exclude = Request.Unvalidated.Form["exclude"];
                TwitterSearchI s = nn.createSearch(include, exclude, title);
                if (nn.addSearch(s))
                {
                    Response.Redirect("/Search/demo");
                    return View("Demo");
                }
                else
                {
                    return View();
                }

            }
            else
            {
                return View();
            }

        }
        public ActionResult Delete()
        {
           
            if (Request.Form["searchSelector"] == null)
            {
                Response.Redirect("/Search/demo");
                return View("Demo");
            }
            int searchId = Convert.ToInt32(Request.Form["searchSelector"]);
            var mm = System.Web.HttpContext.Current.Application["demo"];
            DemoI nn = (DemoI)mm;
            nn.deleteSearchById(searchId);
            Response.Redirect("/Search/demo");
            return View("Demo");
        }
        public ActionResult Result(int searchId = -1)
        {
            if (searchId == -1)
            {
                Response.Redirect("/Search/demo");
                return View("Demo");
            }
            ViewBag.searchId = searchId;
            return View();
        }
        public ActionResult ViewSearch()
        {
            int searchId = 0;
            try
            {
                searchId = Convert.ToInt32(Request.Form["searchSelector"]);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                Response.Redirect("/search/demo");
            }
            var mm = System.Web.HttpContext.Current.Application["demo"];
            DemoI nn = (DemoI)mm;
            TwitterSearchI ts = nn.getSearch(searchId);
            if (ts == null)
            {
                Response.Redirect("/Search/demo");
                return View("Demo");
            }
            ViewBag.ts = ts;
            return View();
        }
        public ActionResult Demo()
        {
            return View();
        }
        public ActionResult AnalyzeDemo()
        {
            return View();
        }
        public ActionResult ViewSelectAnalyze()
        {
            var dates = Request.Form["dates"];
            var searchList = Request.Form["searchList"];
            var laLos = Request.Form["lalos"];
            if (dates == null || searchList == null || laLos == null)
            {
                Response.Redirect("/Search/demo");
                System.Diagnostics.Debug.WriteLine("warning check at ViewSelectAnalyze()");
                return View("Demo");
            }
            var mm = System.Web.HttpContext.Current.Application["demo"];
            DemoI nn = (DemoI)mm;
            // return View("Demo");

            List<List<String>> data = nn.loadTweetsForAnalyzeSelection(dates, searchList, laLos);
            ViewBag.data = data;
            return View();


        }

        [HttpPost]
        public ActionResult ajaxTweets(List<String> ids)
        {

            if (ids == null && ids.Count <= 8)
            {
                return null;
            }
            var mm = System.Web.HttpContext.Current.Application["demo"];
            DemoI nn = (DemoI)mm;
            List<object> tts = nn.loadTweetsForAnalyze(ids.ToArray());
            if (tts == null)
                return null;
            return Json(tts, JsonRequestBehavior.AllowGet);
        }


    }
}