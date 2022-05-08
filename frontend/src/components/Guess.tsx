import * as React from "react";
import { GuessRecord } from "../models/GuessRecord";
import Tile from "./Tile";

export interface IGuessProps {
  guess : GuessRecord;
  opponent: boolean;
}

export default class Guess extends React.Component<IGuessProps> {
  public render() {
    return (
      <div className="tile-container">
        {this.props.guess.word.split("").map((letter, i) => {
          let display_class = "tile";
          if (this.props.guess.score[i] !== 0) {
            if (this.props.guess.score[i] === 1) {
              display_class = `tile tile-match`;
            } else {
              display_class = `tile tile-match-part`;
            }
          }
          return <Tile class_string={display_class} letter={letter} key={i} opponent={this.props.opponent}/>;
        })}
      </div>
    );
  }
}
