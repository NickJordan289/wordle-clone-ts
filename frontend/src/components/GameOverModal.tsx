import * as React from "react";
import { Button, Modal } from "react-bootstrap";

export interface IGameOverModalProps {
  gameOver: boolean;
  isWinner: boolean;
  onClose: () => void;
  onHide: () => void;
}

export default class GameOverModal extends React.Component<IGameOverModalProps> {
  public render() {
    return (
      <Modal show={this.props.gameOver} onHide={this.props.onHide}>
        <Modal.Header closeButton>
          {this.props.isWinner ? (
            <Modal.Title>You Won!</Modal.Title>
          ) : (
            <Modal.Title>Game Over!</Modal.Title>
          )}
        </Modal.Header>
        <Modal.Body>Woohoo, you're reading this text in a modal!</Modal.Body>
        <Modal.Footer>
          <Button
            variant="secondary"
            onClick={() => {
              this.props.onClose();
            }}
          >
            Close
          </Button>
        </Modal.Footer>
      </Modal>
    );
  }
}
