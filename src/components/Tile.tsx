import * as React from "react";

export interface ITileProps {
  letter: string;
  class_string: string;
}

export default class Tile extends React.Component<ITileProps> {
  public render() {
    return (
      <div className={this.props.class_string}>
        <p>{this.props.letter}</p>
      </div>
    );
  }
}
