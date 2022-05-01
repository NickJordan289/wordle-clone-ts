import * as React from "react";

export interface ITileProps {
  letter: string;
  class_string: string;
  opponent: boolean;
}

export default class Tile extends React.Component<ITileProps> {
  public render() {
    return (
      <div className={this.props.class_string}>
        {!this.props.opponent && (<p>{this.props.letter}</p>)}
      </div>
    );
  }
}
