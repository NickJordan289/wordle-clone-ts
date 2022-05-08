import React, { FormEvent, useEffect, useState } from "react";
import "./App.css";
import Guess from "./components/Guess";

interface matchDetail {
  matchId: string,
  host: string,
  guest:string,
  targetWord : string,
}

enum ActionDefinition {
  Default = "Default",
  Join = "Join",
  Leave = "Leave",
  Create = "Create",
  Guess = "Guess",
}

interface ResponseContent {
  content : string,
  action : ActionDefinition
}

function App() {
  const word_difficulty: number = 5;
  const [word, setWord] = useState<string>("");
  const [grid, setGrid] = useState<Array<string>>([]);
  const [opponentGrid, setOpponentGrid] = useState<Array<string>>([]);
  const [targetWord, setTargetWord] = useState<string>("");
  const [newWord, setNewWord] = useState<boolean>(true);
  const [namePrompt, setNamePrompt] = useState<string>(randomString(5));
  const [role, setRole] = useState<string>("client");
  const [groupPrompt, setGroupPrompt] = useState<string>("lobby");
  const [inLobby, setInLobby] = useState<boolean>(true);


  const [isConnected, setIsConnected] = useState<boolean>(false);
  const [ws, setWs] = useState<WebSocket | null>(null);
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
        };
        new_ws.onerror = (e) => {
          console.log("Error with websocket");
          console.log(e)
          setIsConnected(false);
        };
        new_ws.onmessage = (e) => onMessageHandler(e);
        setWs(new_ws);
      });
  }, []);

  const [matchDetails, setMatchDetails] = useState<matchDetail>();
  useEffect(() => {
    ws?.send(
      JSON.stringify({
        from: namePrompt,
        content: word,
        is_system_action: true,
        system_action: `join:${matchDetails?.matchId}`,
      })
    );
  }, [matchDetails]);

  function createGame(): void {
    ws?.send(
      JSON.stringify({
        from: namePrompt,
        content: "",
        action: ActionDefinition.Create,
      })
    );
  }

  function push(w: string): void {
    if (w.length !== word_difficulty) return;
    setOpponentGrid((old) => [...old, w]);
  }

  async function getNewWord(): Promise<string> {
    return fetch(
      `https://random-word-api.herokuapp.com/word?length=${word_difficulty}`
    )
      .then((res) => res.json())
      .then((data) => {
        return data[0];
      });
  }

  useEffect(() => {
    if (newWord === true) {
      getNewWord().then((w) => setTargetWord(w));
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
        action: ActionDefinition.Guess,
      })
    );

    setGrid((grid) => {
      grid.push(word.toLowerCase());
      return grid;
    });
    setWord("");

    if (word.toLowerCase() === targetWord.toLowerCase()) {
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
  }

  function leaveGame(): void {
    ws?.send(
      JSON.stringify({
        from: namePrompt,
        content: "",
        action: ActionDefinition.Leave,
      })
    );
  }

  function onMessageHandler(e: MessageEvent): void {
    let data = JSON.parse(e.data) as ResponseContent;
    console.log("Message received", data);

    switch (data.action) {
      case ActionDefinition.Join:
        setGroupPrompt(data.content);
        setInLobby(data.content === "lobby");
        break;
    
      default:
        console.log("Other");
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
            <p>{role}</p>
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
            <p>{role}</p>
            <p>{matchDetails?.targetWord.length}</p>
            <p>{matchDetails?.targetWord}</p>
            <button type="button" className="btn btn-dark" onClick={leaveGame}>
              Leave Game
            </button>
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
