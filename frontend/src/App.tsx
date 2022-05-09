import React, { FormEvent, useEffect, useState } from "react";
import "./App.css";
import Guess from "./components/Guess";
import { ActionDefinition } from "./models/ActionDefinition";
import { GuessRecord } from "./models/GuessRecord";
import { ResponseContent } from "./models/ResponseContent";

function App() {
  const word_difficulty: number = 5;
  
  const [namePrompt, setNamePrompt] = useState<string>("");
  useEffect(() => {
    let name = localStorage.getItem("name");
    if (name === null) {
      name = randomString(5);
      localStorage.setItem('name', name);
      setNamePrompt(name);
    }
    else {
      setNamePrompt(name);
    }
  }, [])

  const [grid, setGrid] = useState<Array<GuessRecord>>([]);
  const [opponentGrid, setOpponentGrid] = useState<Array<GuessRecord>>([]);
  
  const [word, setWord] = useState<string>("");
  const [groupPrompt, setGroupPrompt] = useState<string>("lobby");

  const [inLobby, setInLobby] = useState<boolean>(true);

  const [isConnected, setIsConnected] = useState<boolean>(false);
  const [ws, setWs] = useState<WebSocket | null>(null);

  /* eslint-disable react-hooks/exhaustive-deps */
  useEffect(() => {
    fetch(
      `https://func-wordle-multiplayer-dev.azurewebsites.net/api/login?userid=${namePrompt}`,
      { method: "POST" }
    )
      .then((res) => res.json())
      .then((data) => {
        let new_ws = new WebSocket(data.url);
        console.log("Initializing websocket...");
        new_ws.onopen = (e) => {
          console.log("Connected to websocket");
          console.log(e)
          setIsConnected(true);
        };
        new_ws.onclose = (e) => {
          console.log("Disconnected from websocket");
          console.log(e)
          setIsConnected(false);
          setInLobby(true);
        };
        new_ws.onerror = (e) => {
          console.log("Error with websocket");
          console.log(e)
          setIsConnected(false);
          setInLobby(true);
        };
        new_ws.onmessage = (e) => onMessageHandler(e);
        setWs(new_ws);
      });
  }, []);

  function createGame(): void {
    ws?.send(
      JSON.stringify({
        from: namePrompt,
        content: "",
        action: ActionDefinition.Create,
      })
    );

    setGrid([]);
    setOpponentGrid([]);
  }

  function pushGuess(g: GuessRecord): void {
    if (g.word === undefined)
      setOpponentGrid((old) => [...old, g]);
    else
      setGrid((old) => [...old, g]);
  }

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
        action: ActionDefinition.Guess,
      })
    );

    setWord("");
  };

  function randomString(length: number): string {
    return Math.random()
      .toString(36)
      .replace(/[^a-z]+/g, "")
      .substring(0, length);
  }

  function joinGame(gameId: string): void {
    ws?.send(
      JSON.stringify({
        from: namePrompt,
        content: gameId,
        action: ActionDefinition.Join,
      })
    );
    setGrid([]);
    setOpponentGrid([]);
  }

  function leaveGame(): void {
    ws?.send(
      JSON.stringify({
        from: namePrompt,
        content: "",
        action: ActionDefinition.Leave,
      })
    );
    setGrid([]);
    setOpponentGrid([]);
  }

  function onMessageHandler(e: MessageEvent): void {
    let data = JSON.parse(e.data) as ResponseContent;
    console.log("Message received", data);

    switch (data.action) {
      case ActionDefinition.Join:
        setGroupPrompt(data.content);
        setInLobby(data.content === "lobby");
        break;
      
      case ActionDefinition.Guess:
        let guess = JSON.parse(data.content) as GuessRecord;
        pushGuess(guess);
        break;
    }
  }

  return (
    <div className="App">
      <div className="App-header">
        <h1>Wordle Multiplayer</h1>
        <h3>Connected: {isConnected ? "True" : "False"}</h3>
        {inLobby ? (
          <div className="lobby">
            <p>Name: {namePrompt}</p>
            <p>In Lobby</p>
            <button type="button" className="btn btn-dark" onClick={createGame}>
              Create Game
            </button>
            <br />
            <div className="input-group mb-3">
              <input
                type="text"
                className="form-control"
                placeholder="Game ID"
                value={groupPrompt}
                onChange={(e) => setGroupPrompt(e.target.value)}
              />
              <button
                className="btn btn-outline-secondary"
                type="button"
                id="button-addon2"
                onClick={(e) => joinGame(groupPrompt)}
              >
                Join Game
              </button>
            </div>
          </div>
        ) : (
          <div className="game">
            <p>Name: {namePrompt}</p>
            <p>In {groupPrompt}</p>
            <button type="button" className="btn btn-dark" onClick={leaveGame}>
              Leave Game
            </button>
            <div className="multiplayer-container">
              <div className="grid-container local-player">
                {grid.map((row, i) => {
                  return (
                    <div key={i}>
                      <Guess
                        guess={row}
                        opponent={false}
                      />
                    </div>
                  );
                })}

                <form>
                  <input
                    type="text"
                    value={word}
                    onChange={handleChange}
                    onSubmit={handleSubmit}
                  />
                  <button onClick={handleSubmit}>Submit</button>
                </form>
              </div>
              <div className="grid-container">
                {opponentGrid.map((row, i) => {
                  return (
                    <div key={i}>
                      <Guess
                        guess={row}
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
