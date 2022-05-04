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
  const [namePrompt, setNamePrompt] = useState<string>(randomString(5));
  const [groupPrompt, setGroupPrompt] = useState<string>("Lobby1");
  const [inLobby, setInLobby] = useState<boolean>(true);
  
  const [ws, setWs] = useState<WebSocket | null>(null);

  function push(w: string) {
    if (w.length !== word_difficulty) return;
    setOpponentGrid((old) => [...old, w]);
  }

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
        from: namePrompt,
        content: word,
        is_system_action: false,
        system_action: "",
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

    useEffect(() => {
      fetch(
        `https://func-wordle-multiplayer-dev.azurewebsites.net/api/login?userid=${namePrompt}`,
        { method: "POST" }
      )
        .then((res) => res.json())
        .then((data) => {
          console.log(data.url);
          let new_ws = new WebSocket(data.url);
          new_ws.onopen = (e) => {
            console.log("Connected to websocket");
            new_ws?.send(
              JSON.stringify({
                from: namePrompt,
                content: "",
                is_system_action: true,
                system_action: `handshake`,
              })
            );
          };
          new_ws.onclose = (e) => {
            console.log("Disconnected from websocket");
          };
          new_ws.onerror = (e) => {
            console.log("Error with websocket");
          };
          new_ws.onmessage = (e) => {
            let data = JSON.parse(e.data);
            console.log("Message received", data);
            if (data.is_system_action) {
              if (
                namePrompt === "host" &&
                data.system_action.includes("handshake")
              ) {
                console.log("Sending out sync");
                new_ws?.send(
                  JSON.stringify({
                    from: namePrompt,
                    content: "",
                    is_system_action: true,
                    system_action: `sync:${targetWord}`,
                  })
                );
              } else if (data.system_action.includes("sync")) {
                let sync_data = data.system_action.split(":");
                setTargetWord(sync_data[1]);
                setOpponentGrid([]);
              }
            } else {
              if (
                data.content &&
                data.from !== namePrompt &&
                data.from !== "[System]"
              ) {
                push(data.content.toLowerCase());
              }
            }
          };
          setWs(new_ws);
        });
  }, []);

  function randomString(length: number) {
    return Math.random().toString(36).replace(/[^a-z]+/g, '').substring(0, length);
  }

  function createGame() {
    // generate random 7 character string
    let game_id = randomString(7);
    setGroupPrompt(game_id);
    joinGame();
    setNamePrompt("host");
  }

  function joinGame() {
    setNamePrompt("client");
    ws?.send(
      JSON.stringify({
        from: namePrompt,
        content: word,
        is_system_action: true,
        system_action: `join:${groupPrompt}`,
      })
    );
    setInLobby(false);
  }

  return (
    <div className="App">
      <div className="App-header">
        {inLobby ? (
          <div className="lobby">
            <p>Name: {namePrompt}</p>
            <p>In Lobby</p>
            <button type="button" className="btn btn-dark" onClick={createGame}>Create Game</button>
            <br/>
            <div className="input-group mb-3">
              <input type="text" className="form-control" placeholder="Game ID" value={groupPrompt} onChange={(e) => setGroupPrompt(e.target.value) }/>
              <button className="btn btn-outline-secondary" type="button" id="button-addon2" onClick={joinGame}>Join Game</button>
            </div>
          </div>
        ) : (
          <div className="game">
            <p>Name: {namePrompt}</p>
            <p>In {groupPrompt}</p>
            <p>{targetWord.length}</p>
            <div className="multiplayer-container">
              <div className="grid-container local-player">
                {grid.map((row, i) => {
                  return (
                    <div key={i}>
                      <Guess
                        word={row}
                        target_word={targetWord}
                        opponent={false}
                      />
                    </div>
                  );
                })}

                <Guess word={word} target_word="" opponent={false} />
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
                      <Guess
                        word={row}
                        target_word={targetWord}
                        opponent={true}
                      />
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default App;
