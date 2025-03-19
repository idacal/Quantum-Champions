using System.Collections.Generic;
using UnityEngine;

namespace EpochLegends
{
    public class EpochGameProperties
    {
        // Constantes para propiedades de Custom Properties
        public const string PLAYER_READY = "PlayerReady";
        public const string PLAYER_LIVES = "PlayerLives";
        public const string PLAYER_LOADED_LEVEL = "PlayerLoadedLevel";
        
        // Valores para vidas (puede no ser relevante para un MOBA, pero lo mantenemos por compatibilidad)
        public const int PLAYER_MAX_LIVES = 3;
        
        // Colores para los equipos y jugadores
        private static readonly Color[] PlayerColors = new Color[] 
        {
            new Color(1.0f, 0.0f, 0.0f),    // Rojo - Equipo Rojo (1)
            new Color(0.0f, 0.2f, 1.0f),    // Azul - Equipo Azul (0)
            new Color(0.8f, 0.2f, 0.0f),    // Naranja - Equipo Rojo (1)
            new Color(0.0f, 0.8f, 1.0f),    // Celeste - Equipo Azul (0)
            new Color(1.0f, 0.0f, 0.5f),    // Rosa - Equipo Rojo (1)
            new Color(0.0f, 0.5f, 1.0f),    // Azul medio - Equipo Azul (0)
            new Color(0.5f, 0.0f, 0.0f),    // Rojo oscuro - Equipo Rojo (1)
            new Color(0.0f, 0.0f, 0.5f),    // Azul oscuro - Equipo Azul (0)
            new Color(1.0f, 0.5f, 0.0f),    // Ámbar - Equipo Rojo (1)
            new Color(0.0f, 1.0f, 0.5f),    // Turquesa - Equipo Azul (0)
        };

        // Obtener color de jugador basado en su número
        public static Color GetColor(int playerNumber)
        {
            int colorIndex = playerNumber % PlayerColors.Length;
            return PlayerColors[colorIndex];
        }
        
        // Obtener equipo basado en el número de jugador (pares e impares)
        public static int GetTeamFromPlayerNumber(int playerNumber)
        {
            // Jugadores con número par pertenecen al equipo azul (0)
            // Jugadores con número impar pertenecen al equipo rojo (1)
            return playerNumber % 2;
        }
    }
}