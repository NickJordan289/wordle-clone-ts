import "bootstrap/dist/css/bootstrap.min.css";
import "./App.css";
import { FormEvent, useEffect, useState } from "react";
import { ActionDefinition } from "./models/ActionDefinition";
import { GuessRecord } from "./models/GuessRecord";
import { ResponseContent } from "./models/ResponseContent";
import Guess from "./components/Guess";
import GameOverModal from "./components/GameOverModal";
import { Spinner } from "react-bootstrap";

function App() {
  const word_difficulty: number = 5;

  const [namePrompt, setNamePrompt] = useState<string>("");
  useEffect(() => {
    let name = localStorage.getItem("name");
    if (name === null) {
      name = randomString(5);
      localStorage.setItem("name", name);
      setNamePrompt(name);
    } else {
      setNamePrompt(name);
    }
  }, []);

  const [grid, setGrid] = useState<Array<GuessRecord>>([]);
  const [opponentGrid, setOpponentGrid] = useState<Array<GuessRecord>>([]);

  const [word, setWord] = useState<string>("");
  const [groupPrompt, setGroupPrompt] = useState<string>("lobby");

  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [inLobby, setInLobby] = useState<boolean>(true);
  const [gameOver, setGameOver] = useState<boolean>(false);
  const [isWinner, setIsWinner] = useState<boolean>(false);

  const [isConnected, setIsConnected] = useState<boolean>(false);
  const [ws, setWs] = useState<WebSocket | null>(null);

  /* eslint-disable react-hooks/exhaustive-deps */
  useEffect(() => {
    if (namePrompt === "" || namePrompt === null || namePrompt === undefined)
      return;
    if (ws !== null) ws?.close();
    fetch(
      `https://func-wordle-multiplayer-dev.azurewebsites.net/api/login?userid=${namePrompt}`,
      {
        method: "POST",
      }
    )
      .then((res) => res.json())
      .then((data) => {
        let new_ws = new WebSocket(data.url);
        console.log("Initializing websocket...");
        new_ws.onopen = (e) => {
          console.log("Connected to websocket");
          console.log(e);
          setIsConnected(true);
        };
        new_ws.onclose = (e) => {
          console.log("Disconnected from websocket");
          console.log(e);
          setIsConnected(false);
          setInLobby(true);
        };
        new_ws.onerror = (e) => {
          console.log("Error with websocket");
          console.log(e);
          setIsConnected(false);
          setInLobby(true);
        };
        new_ws.onmessage = (e) => onMessageHandler(e);
        setWs(new_ws);
      });
  }, [namePrompt]);

  function pushGuess(g: GuessRecord): void {
    if (g.word === undefined || g.word === null || g.word === "")
      setOpponentGrid((old) => [...old, g]);
    else setGrid((old) => [...old, g]);
  }

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
        if (guess.winner) {
          if (
            guess.word === undefined ||
            guess.word === null ||
            guess.word === ""
          ) {
            setIsWinner(false);
            console.log("You lost!");
          } else {
            setIsWinner(true);
            console.log("You won!");
          }
          setGameOver(true);
        }
        break;
    }
    setIsLoading(false);
  }

  function randomString(length: number): string {
    return Math.random()
      .toString(36)
      .replace(/[^a-z]+/g, "")
      .substring(0, length);
  }

  function createGame(): void {
    ws?.send(
      JSON.stringify({
        from: namePrompt,
        content: "",
        action: ActionDefinition.Create,
      })
    );
    setIsLoading(true);
    reset();
  }

  function joinGame(gameId: string): void {
    ws?.send(
      JSON.stringify({
        from: namePrompt,
        content: gameId,
        action: ActionDefinition.Join,
      })
    );
    setIsLoading(true);
    reset();
  }

  function leaveGame(): void {
    ws?.send(
      JSON.stringify({
        from: namePrompt,
        content: "",
        action: ActionDefinition.Leave,
      })
    );
    setIsLoading(true);
    reset();
  }

  function reset(): void {
    setIsWinner(false);
    setGameOver(false);
    setGrid([]);
    setOpponentGrid([]);
  }

  function renderLobby(): JSX.Element {
    return (
      <div className="lobby">
        <p>Name: {namePrompt}</p>
        <p>In Lobby</p>
        <button type="button" className="btn btn-dark" onClick={createGame}>
          Create Game
        </button>
        <br />
        <form
          onSubmit={(e) => {
            e.preventDefault();
            joinGame(groupPrompt);
          }}
        >
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
        </form>
      </div>
    );
  }

  function renderGame(): JSX.Element {
    return (
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
                  <Guess guess={row} opponent={false} />
                </div>
              );
            })}
            <form onSubmit={handleSubmit}>
              <div className="input-group mb-3">
                <input
                  type="text"
                  className="form-control"
                  value={word}
                  onChange={(e) => setWord(e.target.value)}
                />
                <button
                  className="btn btn-outline-secondary"
                  type="button"
                  onClick={handleSubmit}
                >
                  Submit
                </button>
              </div>
            </form>
          </div>
          <div className="grid-container">
            {opponentGrid.map((row, i) => {
              return (
                <div key={i}>
                  <Guess guess={row} opponent={true} />
                </div>
              );
            })}
          </div>
        </div>
      </div>
    );
  }

  function renderLoading(): JSX.Element {
    return (
      <Spinner animation="border" role="status">
        <span className="visually-hidden">Loading...</span>
      </Spinner>
    );
  }

  function renderHandler(): JSX.Element {
    if (inLobby) return renderLobby();
    return renderGame();
  }

  return (
    <div className="App">
      <div className="App-header">
        <h1>Wordle Multiplayer</h1>
        <h3>Connected: {isConnected ? "True" : "False"}</h3>

        {!isConnected || isLoading ? renderLoading() : renderHandler()}

        <GameOverModal
          gameOver={gameOver}
          isWinner={isWinner}
          onClose={leaveGame}
          onHide={() => setGameOver(false)}
        />
      </div>
    </div>
  );
}

export default App;
