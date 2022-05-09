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
        {this.props.guess.score.map((s, i) => {
          let letter = this.props.guess.word ? this.props.guess.word[i] : "";
          let display_class = "tile";
          if (s !== 0) {
            if (s === 1) {
              display_class = `tile tile-match`;
            } else {
              display_class = `tile tile-match-part`;
            }
          }
          return <Tile class_string={display_class} letter={letter} key={i}/>;
        })}
      </div>
    );
  }
}
