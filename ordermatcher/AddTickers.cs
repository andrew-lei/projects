using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

//Make sure to first get the list of symbols from
//http://www.batstrading.com/market_data/symbol_listing/csv/
//Pretty sure link will auto-download csv

namespace AddSecurityApplication {
  public class Security {
    public ObjectId Id { get; set; }
    public string Symbol { get; set; }
  }

  public class Context {
    private MongoDatabase db;

    public Context() {
      MongoClient client = new MongoClient();
      var server = client.GetServer();
      this.db = server.GetDatabase("orders");
    }

    public MongoCollection<Security> Securities {
      get {
        return db.GetCollection<Security>("Security");
      }
    }

    public void AddSecurity(Security mysecurity) {
      this.Securities.Save(mysecurity);
    }
  }

  class Execution {
    static void Main(string[] args) {
      Context ctx = new Context();

      //read the file
      //then add each of the lines in to the security database
      string[] lines = System.IO.File.ReadAllLines(@"./symbol_listing.csv");
      foreach( string line in lines ){
        ctx.AddSecurity( new Security{Symbol = line} );
      }
		}
	}
}
