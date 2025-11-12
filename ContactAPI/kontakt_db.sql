-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Хост: 127.0.0.1
-- Час створення: Лис 05 2025 р., 11:40
-- Версія сервера: 10.4.32-MariaDB
-- Версія PHP: 8.0.30

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- База даних: `kontakt_db`
--

-- --------------------------------------------------------

--
-- Структура таблиці `clients`
--

CREATE TABLE `clients` (
  `Id` int(11) NOT NULL,
  `FullName` longtext NOT NULL,
  `Phone` longtext NOT NULL,
  `Email` longtext NOT NULL,
  `History` longtext NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Дамп даних таблиці `clients`
--

INSERT INTO `clients` (`Id`, `FullName`, `Phone`, `Email`, `History`) VALUES
(1, 'Петро Пастернак', '+380988588654', 'pasternak1963@gmail.com', 'Клієнт створений 21.10.2025 17:21\n🔧 Ремонт №1 — 21.10.2025 17:23 (Статус: Прийнято, Ціна: 1500 грн)'),
(2, 'Анна Мацьків', '+380950014065', 'annmackiv@gmail.com', 'Клієнт створений 29.10.2025 12:55\n🔧 Ремонт №2 — 29.10.2025 12:58 (Статус: Прийнято, Ціна: 1000 грн)');

-- --------------------------------------------------------

--
-- Структура таблиці `repairs`
--

CREATE TABLE `repairs` (
  `Id` int(11) NOT NULL,
  `ClientId` int(11) NOT NULL,
  `DeviceType` longtext NOT NULL,
  `Model` longtext NOT NULL,
  `Problem` longtext NOT NULL,
  `Status` longtext NOT NULL,
  `PartsUsed` longtext NOT NULL,
  `TotalCost` decimal(65,30) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Дамп даних таблиці `repairs`
--

INSERT INTO `repairs` (`Id`, `ClientId`, `DeviceType`, `Model`, `Problem`, `Status`, `PartsUsed`, `TotalCost`, `CreatedAt`) VALUES
(1, 1, 'Ноутбук Acer', 'Nitro V16 AI', 'Не заряджається', 'Прийнято', 'Заміна роз\'єму для зарядки ', 1500.000000000000000000000000000000, '2025-10-21 17:23:29.216609'),
(2, 2, 'Монітор', 'AcerIps FHD', 'Засвіти', 'Готово', 'Заміна шлейфу', 1000.000000000000000000000000000000, '2025-10-29 12:58:18.082349');

-- --------------------------------------------------------

--
-- Структура таблиці `sales`
--

CREATE TABLE `sales` (
  `Id` int(11) NOT NULL,
  `ClientId` int(11) NOT NULL,
  `ServiceId` int(11) NOT NULL,
  `Price` decimal(65,30) NOT NULL,
  `Date` datetime(6) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Структура таблиці `services`
--

CREATE TABLE `services` (
  `Id` int(11) NOT NULL,
  `Name` longtext NOT NULL,
  `Description` longtext NOT NULL,
  `Price` decimal(65,30) NOT NULL,
  `Category` longtext NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Структура таблиці `users`
--

CREATE TABLE `users` (
  `Id` int(11) NOT NULL,
  `Username` longtext NOT NULL,
  `PasswordHash` longtext NOT NULL,
  `Email` longtext NOT NULL,
  `Role` longtext NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Дамп даних таблиці `users`
--

INSERT INTO `users` (`Id`, `Username`, `PasswordHash`, `Email`, `Role`) VALUES
(1, 'Anastasiia', 'C86+AWk8RDjXWMC+gYXb0mis8dHA8wH+Nrqyh0OFd9o=', 'anastasiya.pashkovska@kpk-lp.com.ua', 'admin'),
(2, 'Admin', 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', 'admin@gmail.com', 'admin');

-- --------------------------------------------------------

--
-- Структура таблиці `__efmigrationshistory`
--

CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Дамп даних таблиці `__efmigrationshistory`
--

INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`) VALUES
('20251021141159_InitClean', '9.0.10');

--
-- Індекси збережених таблиць
--

--
-- Індекси таблиці `clients`
--
ALTER TABLE `clients`
  ADD PRIMARY KEY (`Id`);

--
-- Індекси таблиці `repairs`
--
ALTER TABLE `repairs`
  ADD PRIMARY KEY (`Id`);

--
-- Індекси таблиці `sales`
--
ALTER TABLE `sales`
  ADD PRIMARY KEY (`Id`);

--
-- Індекси таблиці `services`
--
ALTER TABLE `services`
  ADD PRIMARY KEY (`Id`);

--
-- Індекси таблиці `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`Id`);

--
-- Індекси таблиці `__efmigrationshistory`
--
ALTER TABLE `__efmigrationshistory`
  ADD PRIMARY KEY (`MigrationId`);

--
-- AUTO_INCREMENT для збережених таблиць
--

--
-- AUTO_INCREMENT для таблиці `clients`
--
ALTER TABLE `clients`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT для таблиці `repairs`
--
ALTER TABLE `repairs`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT для таблиці `sales`
--
ALTER TABLE `sales`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблиці `services`
--
ALTER TABLE `services`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблиці `users`
--
ALTER TABLE `users`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
