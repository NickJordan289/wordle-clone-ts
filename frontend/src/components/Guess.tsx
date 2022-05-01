import * as React from "react";
import Tile from "./Tile";

export interface IGuessProps {
  word: string;
  target_word: string;
  opponent: boolean;
}

export default class Guess extends React.Component<IGuessProps> {
  public render() {
    return (
      <div className="tile-container">
        {this.props.word.split("").map((letter, i) => {
          const target_word = this.props.target_word.toLowerCase();
          let in_word = target_word.split("").indexOf(letter) > -1;

          let display_class = "tile";
          if (in_word) {
            if (i === target_word.split("").indexOf(letter)) {
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
