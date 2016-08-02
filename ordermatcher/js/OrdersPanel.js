import React from "react";
import ReactDOM from "react-dom";
import { FormControl, Col, Row } from "react-bootstrap";
var $ = require('jquery');

import ListEl from "./ListEl";

export default class OrdersPanel extends React.Component {
  constructor() {
    super();
    this.getOrders();
    this.state = {
      list: [],
      secs: [],
      sec: {}
    };
  }

  getOrders() {
    var context = this;
    console.log("testing");
    $.ajax({
        url: "http://andrewlei.org:8888/ordersec",
        type: "get",
        datatype:"json",
        success: function(response){
            var data = JSON.parse(response);
            context.setState({list: data.map( x => <ListEl key={x} value={x}/>)});
        }
    });
  }

  getSecOrders(security) {
    var context = this;
    $.ajax({
        url: "http://andrewlei.org:8888/ordersec/"  + security.target.value,
        type: "get",
        datatype:"json",
        success: function(response){
            var data = JSON.parse(response);
            context.setState({secs: data.map( x => <ListEl key={x} value={x}/>)});
        }
    });
  }

  getOrder(order) {
    var context = this;
    $.ajax({
        url: "http://andrewlei.org:8888/orders/" + order.target.value,
        type: "get",
        datatype:"json",
        success: function(response){
            var data = JSON.parse(response);
            context.setState({sec: {
              security: data["Security"],
              side: data["Side"],
              time: (new Date(parseInt(data["Time"].match(/\d+/g)[0])).toString()),
              quantity: data["Quantity"],
              outstanding: data["Outstanding"],
              price: parseFloat(data["Price"] / 100.0).toFixed(2)
            }});
        }
    });
  }

  render() {
    return (
      <Row className="show-grid">
        <Col xs={8} sm={4}>
          <FormControl componentClass="select" size="10" onChange={this.getSecOrders.bind(this)}>
            {this.state.list}
          </FormControl>
        </Col>
        <Col xs={8} sm={4}>
          <FormControl componentClass="select" size="10" onChange={this.getOrder.bind(this)}>
            {this.state.secs}
          </FormControl>
        </Col>
        <Col xs={8} sm={4}>
          <h4><b>Security: {this.state.sec.security}</b></h4>
          Side: {this.state.sec.side}<br/>
          Time: {this.state.sec.time}<br/>
          Quantity: {this.state.sec.quantity}<br/>
          Outstanding: {this.state.sec.outstanding}<br/>
          Price: {this.state.sec.price}
        </Col>
      </Row>
    );
  }
}
