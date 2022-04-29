import React, { FormEvent, useEffect, useState } from "react";
import "./App.css";
import Guess from "./components/Guess";

function App() {
  const word_difficulty: number = 5;
  const [word, setWord] = useState<string>("");
  const [grid, setGrid] = useState<Array<string>>([]);
  const [targetWord, setTargetWord] = useState<string>("");
  const [newWord, setNewWord] = useState<boolean>(true);

  function getNewWord() {
    fetch(
      `https://random-word-api.herokuapp.com/word?length=${word_difficulty}`
    )
      .then((res) => res.json())
      .then((data) => {
        setTargetWord(data[0]);
      });
  }

  useEffect(() => {
    if (newWord === true) {
      getNewWord();
      setNewWord(false);
    }
  }, [newWord]);

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setWord(event.target.value);
  };

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();

    if (word.length !== 5) return;

    const target_word = targetWord;
    setGrid((grid) => {
      grid.push(word.toLowerCase());
      return grid;
    });
    setWord("");

    if (word.toLowerCase() === target_word.toLowerCase()) {
      alert("Correct!");
      setNewWord(true);
      setGrid([]);
    }
  };

  const handleReset = (event: FormEvent) => {
    event.preventDefault();
    setWord("");
    setGrid([]);
    setNewWord(true);
  };

  return (
    <div className="App">
      <div className="App-header">
        <p>{targetWord.length}</p>
        <div className="gridContainer">
          {grid.map((row, i) => {
            return (
              <div key={i}>
                <Guess word={row} target_word={targetWord} />
              </div>
            );
          })}
        </div>
        <Guess word={word} target_word="" />
        <form>
          <input
            type="text"
            value={word}
            onChange={handleChange}
            onSubmit={handleSubmit}
          />
          <button onClick={handleSubmit}>Submit</button>
          <button onClick={handleReset}>Reset</button>
        </form>
      </div>
    </div>
  );
}

export default App;
