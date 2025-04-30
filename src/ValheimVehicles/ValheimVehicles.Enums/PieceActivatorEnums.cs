namespace ValheimVehicles.Enums;

public enum InitializationState
{
  Pending, // when the ship has a pending state
  Complete, // when the ship loads as an existing ship and has pieces.
  Created // when the ship is created with 0 pieces
}

public enum PendingPieceStateEnum
{
  Idle, // not started
  Scheduled, // called but not started
  Running, // running
  Failure, // failed
  Complete, // completed successfully
  ForceReset // forced to exit IE teleport or despawn or logout or command to destroy it.
}