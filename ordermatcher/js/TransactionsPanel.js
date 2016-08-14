import React from "react";
import ReactDOM from "react-dom";
import { FormControl, Col, Row, Alert } from "react-bootstrap";
var $ = require('jquery');

import ListEl from "./ListEl";

export default class TransactionsPanel extends React.Component {
  constructor() {
    super();
    this.getTransactions();
    this.state = {
      list: [],
      secs: [],
      sec: {}
    };
  }

  getTransactions() {
    var context = this;
    $.ajax({
        url: "http://andrewlei.org:8888/transactionsec",
        type: "get",
        datatype:"json",
        success: function(response){
            var data = JSON.parse(response);
            context.setState({list: data.map( x => <ListEl key={x} value={x}/>)});
        }
    });
  }

  getSecTransactions(security) {
    var context = this;
    $.ajax({
        url: "http://andrewlei.org:8888/transactionsec/"  + security.target.value,
        type: "get",
        datatype:"json",
        success: function(response){
            var data = JSON.parse(response);
            context.setState({secs: data.map( x => <ListEl key={x} value={x}/>)});
        }
    });
  }

  getTransaction(order) {
    var context = this;
    $.ajax({
        url: "http://andrewlei.org:8888/transactions/" + order.target.value,
        type: "get",
        datatype:"json",
        success: function(response){
            var data = JSON.parse(response);
            context.setState({sec: {
              security: data["Security"],
              buyid: data["BuyId"],
              sellid: data["SellId"],
              time: (new Date(parseInt(data["Time"].match(/\d+/g)[0])).toString()),
              quantity: data["Quantity"],
              buyprice: parseFloat(data["BuyPrice"] / 100.0).toFixed(2),
              sellprice: parseFloat(data["SellPrice"] / 100.0).toFixed(2)
            }});
        }
    });
  }

  render() {
    return (
      <Row className="show-grid">
        <Col xs={8} sm={4}>
          <FormControl componentClass="select" size="10" onChange={this.getSecTransactions.bind(this)}>
            {this.state.list}
          </FormControl>
        </Col>
        <Col xs={8} sm={4}>
          <FormControl componentClass="select" size="10" onChange={this.getTransaction.bind(this)}>
            {this.state.secs}
          </FormControl>
        </Col>
        <Col xs={8} sm={4}>
          <Alert>
            <h4><b>Security: {this.state.sec.security}</b></h4>
            Buy ID: {this.state.sec.buyid}<br/>
            Sell ID: {this.state.sec.sellid}<br/>
            Time: {this.state.sec.time}<br/>
            Quantity: {this.state.sec.quantity}<br/>
            Buy Price: {this.state.sec.buyprice}<br/>
            Buy Price: {this.state.sec.sellprice}
          </Alert>
        </Col>
      </Row>
    );
  }
}
