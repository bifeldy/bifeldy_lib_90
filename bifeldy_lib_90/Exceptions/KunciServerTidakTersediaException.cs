namespace bifeldy_lib_90.Exceptions {

    public sealed class KunciServerTidakTersediaException : Exception {

        public KunciServerTidakTersediaException() { }

        public KunciServerTidakTersediaException(string message) : base(message) { }

        public KunciServerTidakTersediaException(string message, Exception inner) : base(message, inner) { }

    }

}