using System;

[Serializable]
class PeacefulPieException : Exception {
    public PeacefulPieException(string? message = null) : base(message) {
    }
}
