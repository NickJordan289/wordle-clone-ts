import React, { FormEvent, useEffect, useState } from "react";
import "./App.css";
import Guess from "./components/Guess";

function App() {
  const word_difficulty: number = 5;
  const [word, setWord] = useState<string>("");
  const [grid, setGrid] = useState<Array<string>>([]);
  const [opponentGrid, setOpponentGrid] = useState<Array<string>>([]);
  const [targetWord, setTargetWord] = useState<string>("");
  const [newWord, setNewWord] = useState<boolean>(true);
  const [ws, setWs] = useState<WebSocket | null>(null);

  function push (w: string) {
    if (w.length !== word_difficulty) return;
    setOpponentGrid((old) => [...old, w]);
  }

  useEffect(() => {
    fetch("https://func-wordle-multiplayer-dev.azurewebsites.net/api/login?userid=1", { method: "POST" })
      .then((res) => res.json())
      .then((data) => {
        console.log(data.url);
        let new_ws = new WebSocket(data.url);
        new_ws.onopen = (e) => {
          console.log("Connected to websocket");
        };
        new_ws.onclose = (e) => {
          console.log("Disconnected from websocket");
        };
        new_ws.onerror = (e) => {
          console.log("Error with websocket");
        };
        new_ws.onmessage = (e) => {
          let data = JSON.parse(e.data);
          if (data.content && data.from !== "1" && data.from !== "[System]") {
            console.log("Message received", data);
            push(data.content.toLowerCase());
          }
        };
        setWs(new_ws);
      });
    
  }, []);

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

    if (word.length !== word_difficulty) return;

    ws?.send(
      JSON.stringify({
        from: "1",
        content: word,
      })
    );

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
        <div className="multiplayer-contianer">
          <div className="grid-container local-player">
            {grid.map((row, i) => {
              return (
                <div key={i}>
                  <Guess word={row} target_word={targetWord} opponent={false}/>
                </div>
              );
            })}

            <Guess word={word} target_word="" opponent={false}/>
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
          <div className="grid-container">
            {opponentGrid.map((row, i) => {
              return (
                <div key={i}>
                  <Guess word={row} target_word={targetWord} opponent={true}/>
                </div>
              );
            })}
          </div>
        </div>
        <div></div>
      </div>
    </div>
  );
}

export default App;
