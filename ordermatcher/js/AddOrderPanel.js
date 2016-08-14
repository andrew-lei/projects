import React from "react";
import ReactDOM from "react-dom";
import { FormGroup, FormControl, DropdownButton, MenuItem, InputGroup, Button, Alert, Col } from "react-bootstrap";
var $ = require('jquery');

import ListEl from "./ListEl";

function capitalizeFirstLetter(string) {
    return string[0].toUpperCase() + string.slice(1);
}

export default class AddOrderPanel extends React.Component {
  constructor() {
    super();
    this.state = {
      side: "Side",
      alertdisplay: "hidden",
      added: "danger"
    }
  }

  addOrder() {
    var context = this;
    var data = {
      "Side": this.state.side,
      "Security": this.state.security,
      "Quantity": this.state.quantity,
      "Price": parseInt(100 * this.state.price)
    };
    $.ajax({
      url: "http://andrewlei.org:8888/addorder",
      type: "post",
      datatype:"json",
      data: data,
      success: function(response) {
        context.setState({message: response});
        context.setState({added: "success"});
        context.setState({alertdisplay: ""});
      },
      error: function(response) {
        context.setState({message: "Error adding order; order not added."});
        context.setState({added: "danger"});
        context.setState({alertdisplay: ""});
      }
    });

  }

  render() {
    var context = this;
    return (
      <form>
        <FormGroup>
          <center>
          <Col xs={8} sm={6} xsOffset={3}>
            <Alert bsStyle={this.state.added} className={this.state.alertdisplay}>
              <button
                type="button"
                class="close"
                onClick={function(){context.setState({alertdisplay: "hidden"})}}
              >
                &#215;
              </button>
              {this.state.message}
            </Alert>

            <InputGroup>
              <DropdownButton
                componentClass={InputGroup.Button}
                id="input-dropdown-addon"
                title={capitalizeFirstLetter(this.state.side)}
                onSelect={function(event){context.setState({side: event})}}
              >
                <MenuItem eventKey="buy" key="1">Buy</MenuItem>
                <MenuItem eventKey="sell" key="2">Sell</MenuItem>
              </DropdownButton>
              <FormControl
                value={this.state.security}
                type="text"
                placeholder="Security"
                onChange={function(event){context.setState({security: event.target.value})}}
              />
            </InputGroup>

            <FormControl
              value={this.state.quantity}
              type="text"
              placeholder="Quantity"
              onChange={function(event){context.setState({quantity: event.target.value})}}
            />

            <InputGroup>
              <InputGroup.Addon>$</InputGroup.Addon>
              <FormControl
                value={this.state.price}
                type="text"
                placeholder="Price"
                onChange={function(event){context.setState({price: event.target.value})}}
              />
            </InputGroup>

            <Button onClick={this.addOrder.bind(this)}>
              Add
            </Button>
            </Col>
          </center>
        </FormGroup>
      </form>
    );
  }
}
