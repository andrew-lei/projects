import React from "react";
import ReactDOM from "react-dom";

export default class ListEl extends React.Component {
  constructor() {
    super();
  }

  render() {
    return (
      <option value={this.props.value}>{this.props.value}</option>
    );
  }
}
