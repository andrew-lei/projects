using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Mono.Unix;
using Mono.Unix.Native;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.ModelBinding;


namespace OrderMatcher {
  public class Security {
    public ObjectId Id { get; set; }
    public string Symbol { get; set; }
  }

  public class Order {
    public ObjectId Id { get; set; }

    private string security;
    public string Security {
      get {
        return security;
      }
      set {
        security = value.ToUpper();
      }
    }

    private string side;
    public string Side {
      get {
        return side;
      }
      set {
        if( value != "buy" && value != "sell" ){
          throw new ArgumentException("value", "must be either buy or sell");
        }
        side = value;
      }
    }

    private int quantity;
    public int Quantity {
      get {
        return quantity;
      }
      set {
        if((int)value <= 0) {
          throw new ArgumentOutOfRangeException("value", "must be a positive number");
        }
        quantity = (int)value;
      }
    }

    public int? Outstanding { get; set; }

    //decimal doesn't work because MongoDB does not have decimals only floating-point
    //this will then be cast to string which is unsuitable for comparisons and sorting
    private int price;
    public int Price {
      get {
        return price;
      }
      set {
        if((int)value <= 0) {
          throw new ArgumentOutOfRangeException("value", "must be a positive number");
        }
        price = (int)value;
      }
    }
    public DateTime Time = DateTime.Now;

    public string ToJson() {
      return "{\"Time\":" +
        new JavaScriptSerializer().Serialize(Time) +
        ",\"Id\":\"" +
        Id.ToString() +
        "\",\"Security\":\"" +
        Security.ToString() +
        "\",\"Side\":\"" +
        Side.ToString() +
        "\",\"Quantity\":" +
        Quantity.ToString() +
        ",\"Outstanding\":" +
        Outstanding.ToString() +
        ",\"Price\":" +
        Price.ToString() +
        "}";
    }
  }

  public class Transaction {
    public ObjectId Id { get; set; }
    public string Security { get; set; }
    public ObjectId BuyId { get; set; }
    public ObjectId SellId { get; set; }
    public int? Quantity { get; set; }
    public int BuyPrice { get; set; }
    public int SellPrice { get; set; }
    public DateTime Time = DateTime.Now;

    public string ToJson() {
      return "{\"Time\":" +
        new JavaScriptSerializer().Serialize(Time) +
        ",\"Id\":\"" + Id.ToString() +
        "\",\"Security\":\"" +
        Security.ToString() +
        "\",\"BuyId\":\"" +
        BuyId.ToString() +
        "\",\"SellId\":\"" +
        SellId.ToString() +
        "\",\"Quantity\":" +
        Quantity.ToString() +
        ",\"BuyPrice\":" +
        BuyPrice.ToString() +
        ",\"SellPrice\":" +
        SellPrice.ToString() +
        "}";
    }
  }

  public class Context {
    private MongoDatabase db;

    public Context() {
      MongoClient client = new MongoClient();
      var server = client.GetServer();
      this.db = server.GetDatabase("orders");
    }

    public MongoCollection<Order> Orders {
      get {
        return db.GetCollection<Order>("Order");
      }
    }

    public MongoCollection<Transaction> Transactions {
      get {
        return db.GetCollection<Transaction>("Transaction");
      }
    }

    public MongoCollection<Security> Securities {
      get {
        return db.GetCollection<Security>("Security");
      }
    }

    public void AddOrder(Order myorder) {
      //save at beginning to initialise an Id
      //save at end to update Outstanding
      this.Orders.Save(myorder);
      List<Order> matches;
      if(string.Equals(myorder.Side,"buy")) {
        matches = this.Orders.
          Find( Query.And(
            Query<Order>.EQ(p => p.Security, myorder.Security),
            Query<Order>.EQ(p => p.Side, "sell"),
            Query<Order>.LTE(p => p.Price, myorder.Price),
            Query<Order>.GT(p => p.Outstanding, 0)) ).
          SetSortOrder(SortBy<Order>.Ascending(p => p.Price)).
          ToList();
        int total = matches.Count();
        int index = 0;
        int? lesser;
        Transaction mytransaction;
        while( myorder.Outstanding > 0 && index < total ) {
          lesser = (myorder.Outstanding < matches[index].Outstanding) ?
            myorder.Outstanding : matches[index].Outstanding;
          myorder.Outstanding -= lesser;
          matches[index].Outstanding -= lesser;
          mytransaction = new Transaction {
            Security = myorder.Security,
            BuyId = myorder.Id,
            SellId = matches[index].Id,
            Quantity = lesser,
            BuyPrice = myorder.Price,
            SellPrice = matches[index].Price
          };
          this.Orders.Save(matches[index++]);
          this.Transactions.Save(mytransaction);
        }
      }
      else {
        matches = this.Orders.
          Find( Query.And(
            Query<Order>.EQ(p => p.Security, myorder.Security),
            Query<Order>.EQ(p => p.Side, "buy"),
            Query<Order>.GTE(p => p.Price, myorder.Price),
            Query<Order>.GT(p => p.Outstanding, 0)) ).
          SetSortOrder(SortBy<Order>.Ascending(p => p.Price)).
          ToList();
        int total = matches.Count();
        int index = 0;
        int? lesser;
        Transaction mytransaction;
        while( myorder.Outstanding > 0 && index < total ) {
          lesser = (myorder.Outstanding < matches[index].Outstanding) ?
            myorder.Outstanding : matches[index].Outstanding;
          myorder.Outstanding -= lesser;
          matches[index].Outstanding -= lesser;
          mytransaction = new Transaction {
            Security = myorder.Security,
            BuyId = matches[index].Id,
            SellId = myorder.Id,
            Quantity = lesser,
            BuyPrice = matches[index].Price,
            SellPrice = myorder.Price
          };
          this.Orders.Save(matches[index++]);
          this.Transactions.Save(mytransaction);
        }
      }
      this.Orders.Save(myorder);
    }
  }

  class Program {
    static void Main(string[] args) {
      var uri = "http://localhost:8888";
      Console.WriteLine("Starting Nancy on " + uri);
      StaticConfiguration.DisableErrorTraces = false;

      // initialize an instance of NancyHost
      var host = new NancyHost(new Uri(uri));
      host.Start();  // start hosting

      // check if we're running on mono
      if (Type.GetType("Mono.Runtime") != null) {
        // on mono, processes will usually run as daemons - this allows you to listen
        // for termination signals (ctrl+c, shutdown, etc) and finalize correctly
        UnixSignal.WaitAny(new[] {
          new UnixSignal(Signum.SIGINT),
          new UnixSignal(Signum.SIGTERM),
          new UnixSignal(Signum.SIGQUIT),
          new UnixSignal(Signum.SIGHUP)
        });
      }
      else {
        Console.ReadLine();
      }

      Console.WriteLine("Stopping Nancy");
      host.Stop();  // stop hosting
    }
  }

  public static class NancyExtensions {
    public static void EnableCors(this NancyModule module)
    {
      module.After.AddItemToEndOfPipeline(x => {
        x.Response.WithHeader("Access-Control-Allow-Origin", "*");
      });
    }
  }

  public class Testing {
    public string Entry;
  }

  public class OrderModule : NancyModule {
    ObjectId RetId(Order myorder) {
      return myorder.Id;
    }

    string FindOrder(Context ctx, string Id) {
      return ctx.
        Orders.
        Find(Query<Order>.EQ(p => p.Id, ObjectId.Parse(Id))).
        First().ToJson();
    }

    string FindTransaction(Context ctx, string Id) {
      return ctx.
        Transactions.
        Find(Query<Transaction>.EQ(p => p.Id, ObjectId.Parse(Id))).
        First().ToJson();
    }

    public OrderModule() {
      //To provide 'Access-Control-Allow-Origin' header
      this.EnableCors();
      Context ctx = new Context();
      Get["/orders"] = _ => new JavaScriptSerializer().Serialize(ctx.
        Orders.Distinct("_id").
        ToList().
        Select(i => i.ToString()).
        ToList() ).
        ToString();
      Get["/orders/{id}"] = _ => FindOrder(ctx, _.id);
      Get["/transactions"] = _ => new JavaScriptSerializer().Serialize(ctx.
        Transactions.Distinct("_id").
        ToList().
        Select(i => i.ToString()).
        ToList() ).
        ToString();
      Get["/transactions/{id}"] = _ => FindTransaction(ctx, _.id);
      Post["/addorder"] = _ => {
        Order neworder = this.Bind<Order>();
        neworder.Outstanding = neworder.Quantity;
        if( ctx.Securities.Find(Query<Security>.EQ(p => p.Symbol, neworder.Security.ToUpper())).ToList().Count() > 0 ) {
          ctx.AddOrder(neworder);
          return "Order successfully added! Id " + neworder.Id;
        }
        else {
          throw new ArgumentException(neworder.Security, "is not in the Securities database.");
        }
      };
    }
  }
}
