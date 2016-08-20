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
  //security, e.g., GOOG
  public class Security {
    public ObjectId Id { get; set; }
    public string Symbol { get; set; }
  }

  //Order details are what security it is, buy or sell, quantity price, and
  //when order was made. Outstanding quantity says how much is remaining.
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

    //Decimal doesn't work because MongoDB does not have decimals, only floating-point.
    //This will then be cast to string which is unsuitable for comparisons and sorting.
    //For this reason I use integers, with the input multiplied by a hundred
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

  //Transaction details include name of security, the IDs of the
  //buy and sell order, the quantity filled by the order, the
  //buying and selling price, and the time of transaction
  public class Transaction {
    public ObjectId Id { get; set; }
    public string Security { get; set; }
    public ObjectId BuyId { get; set; }
    public ObjectId SellId { get; set; }
    //nullable because Order.Outstanding is nullable
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
      //save at beginning to initialise an Id for myorder
      //save at end to update Outstanding
      this.Orders.Save(myorder);
      List<Order> matches;

      //If the order is to buy
      if(string.Equals(myorder.Side,"buy")) {
        //find orders matching the following criteria:
        //1) same security
        //2) side sell
        //3) price <= price of myorder.Price
        //4) quantity > 0
        //and then sort so that price is ascending; the cheapest get chosen first
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

        //lesser is to keep track of which outstanding amount there is less of
        int? lesser;
        Transaction mytransaction;

        //for each order in our matches, or at least until the order we're
        //adding is filled
        while( myorder.Outstanding > 0 && index < total ) {
          //minimum of myorder.Outstanding and Outstanding of match
          lesser = (myorder.Outstanding < matches[index].Outstanding) ?
            myorder.Outstanding : matches[index].Outstanding;

          //subtract lesser from both
          myorder.Outstanding -= lesser;
          matches[index].Outstanding -= lesser;

          //create new transaction
          mytransaction = new Transaction {
            Security = myorder.Security,
            BuyId = myorder.Id,
            SellId = matches[index].Id,
            Quantity = lesser,
            BuyPrice = myorder.Price,
            SellPrice = matches[index].Price
          };

          //update match and increment index
          this.Orders.Save(matches[index++]);

          //add transaction
          this.Transactions.Save(mytransaction);
        }
      }
      //the order is to sell
      else {
        //find orders matching the following criteria:
        //1) same security
        //2) side buy
        //3) price >= price of myorder.Price
        //4) quantity > 0
        //and then sort so that price is descending; the highest bidders get chosen first
        matches = this.Orders.
          Find( Query.And(
            Query<Order>.EQ(p => p.Security, myorder.Security),
            Query<Order>.EQ(p => p.Side, "buy"),
            Query<Order>.GTE(p => p.Price, myorder.Price),
            Query<Order>.GT(p => p.Outstanding, 0)) ).
          SetSortOrder(SortBy<Order>.Descending(p => p.Price)).
          ToList();
        int total = matches.Count();
        int index = 0;

        //lesser is to keep track of which outstanding amount there is less of
        int? lesser;
        Transaction mytransaction;

        //for each order in our matches, or at least until the order we're
        //adding is filled
        while( myorder.Outstanding > 0 && index < total ) {
          //minimum of myorder.Outstanding and Outstanding of match
          lesser = (myorder.Outstanding < matches[index].Outstanding) ?
            myorder.Outstanding : matches[index].Outstanding;

          //subtract lesser from both
          myorder.Outstanding -= lesser;
          matches[index].Outstanding -= lesser;

          //create new transaction
          mytransaction = new Transaction {
            Security = myorder.Security,
            BuyId = matches[index].Id,
            SellId = myorder.Id,
            Quantity = lesser,
            BuyPrice = matches[index].Price,
            SellPrice = myorder.Price
          };

          //update match and increment index
          this.Orders.Save(matches[index++]);

          //add transaction
          this.Transactions.Save(mytransaction);
        }
      }

      //update order; the outstanding amount may have changed
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

  public class OrderModule : NancyModule {
    //this returns an ID
    ObjectId RetId(Order myorder) {
      return myorder.Id;
    }

    //This finds an order with given ID
    //Returns order details in JSON
    string FindOrder(Context ctx, string Id) {
      return ctx.
        Orders.
        Find(Query<Order>.EQ(p => p.Id, ObjectId.Parse(Id))).
        First().ToJson();
    }

    //This finds a transaction with given ID
    //Returns transaction details in JSON
    string FindTransaction(Context ctx, string Id) {
      return ctx.
        Transactions.
        Find(Query<Transaction>.EQ(p => p.Id, ObjectId.Parse(Id))).
        First().ToJson();
    }

    //This gets all of the order IDs involving a given security
    string GetOrders(Context ctx, string sec) {
      return new JavaScriptSerializer().Serialize(ctx.
        Orders.
        Distinct("_id", Query<Order>.EQ(p => p.Security, sec)).
        ToList().
        Select(i => i.ToString()).
        OrderBy(x => x) ).
        ToString();
    }

    //This gets all of the transaction IDs involving a given security
    string GetTransactions(Context ctx, string sec) {
      return new JavaScriptSerializer().Serialize(ctx.
        Transactions.
        Distinct("_id", Query<Order>.EQ(p => p.Security, sec)).
        ToList().
        Select(i => i.ToString()).
        OrderBy(x => x) ).
        ToString();
    }

    public OrderModule() {
      //To provide 'Access-Control-Allow-Origin' header
      this.EnableCors();
      Context ctx = new Context();

      //list of orders
      Get["/orders"] = _ => new JavaScriptSerializer().Serialize(ctx.
        Orders.Distinct("_id").
        ToList().
        Select(i => i.ToString()).
        ToList() ).
        ToString();

      //the order with given ID
      Get["/orders/{id}"] = _ => FindOrder(ctx, _.id);

      //list of securities ordered
      Get["/ordersec"] = _ => new JavaScriptSerializer().Serialize(ctx.
        Orders.
        Distinct("Security").
        ToList().
        Select(i => i.ToString()).
        OrderBy(i => i)).
        ToString();

      //gets all of the order IDs involving a given security
      Get["/ordersec/{security}"] = _ => GetOrders(ctx, _.security);

      //list of transactions
      Get["/transactions"] = _ => new JavaScriptSerializer().Serialize(ctx.
        Transactions.Distinct("_id").
        ToList().
        Select(i => i.ToString()).
        ToList() ).
        ToString();

      //transaction with a given ID
      Get["/transactions/{id}"] = _ => FindTransaction(ctx, _.id);

      //list of securities transacted
      Get["/transactionsec"] = _ => new JavaScriptSerializer().Serialize(ctx.
        Transactions.
        Distinct("Security").
        ToList().
        Select(i => i.ToString()).
        OrderBy(i => i)).
        ToString();

      //gets all of the transaction IDs involving a given security
      Get["/transactionsec/{security}"] = _ => GetTransactions(ctx, _.security);

      //add an order to the database
      //fulfil some orders
      Post["/addorder"] = _ => {

        //create new order with front-end data
        Order neworder = this.Bind<Order>();

        //Outstanding initialised with full quantity
        neworder.Outstanding = neworder.Quantity;

        //only do add if its security shows up in the database
        if( ctx.Securities.Find(Query<Security>.EQ(p => p.Symbol, neworder.Security.ToUpper())).ToList().Count() > 0 ) {
          ctx.AddOrder(neworder);
          return "Order successfully added! Id " + neworder.Id;
        }
        //otherwise don't add, throw an exception
        else {
          throw new ArgumentException(neworder.Security, "is not in the Securities database.");
        }
      };
    }
  }
}
