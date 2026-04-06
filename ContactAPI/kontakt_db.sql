-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Хост: 127.0.0.1:3306
-- Час створення: Квт 06 2026 р., 14:51
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
  `History` longtext NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Дамп даних таблиці `clients`
--

INSERT INTO `clients` (`Id`, `FullName`, `Phone`, `Email`, `History`, `CreatedAt`) VALUES
(1, 'Анна Мацьків', '+380950014065', 'annmackiv@gmail.com', 'Клієнт створений 29.10.2025 12:55\n🔧 Ремонт №2 — 29.10.2025 12:58 (Статус: Прийнято, Ціна: 1000 грн)', '2025-11-12 00:05:18'),
(2, 'Петро Коваленко', '+380671234567', 'petro.demo@example.com', '', '2025-11-12 00:05:18'),
(3, 'Марія Гнатів', '+380931112233', 'maria.demo@example.com', '', '2025-11-12 00:05:18'),
(4, 'Олександр Романюк', '+380506547894', 'olex.demo@example.com', '', '2025-11-12 00:05:18'),
(5, 'Наталія Демчук', '+380681234879', 'natalia.demo@example.com', '', '2025-11-12 00:05:18'),
(6, 'Ігор Левченко', '+380991112299', 'igor.demo@example.com', '', '2025-11-12 00:05:18'),
(7, 'Галина Остапчук', '+380635556677', 'galyna.demo@example.com', '', '2025-11-12 00:05:18'),
(8, 'Світлана Бойко', '+380977774455', 'svitlana.demo@example.com', '', '2025-11-12 00:05:18'),
(9, 'Руслан Шевченко', '+380667778899', 'ruslan.demo@example.com', '', '2025-11-12 00:05:18'),
(10, 'Олена Пастух', '+380934445566', 'olena.demo@example.com', '', '2025-11-12 00:05:18'),
(11, 'Микола Яремчук ', '+380507778855', 'mykola.demo@example.com', '', '2025-11-12 00:05:18'),
(21, 'Труш Олександр ', '+380987652519', 'trysh@gmail.com', 'Клієнт створений 21.03.2026 12:43', '2026-03-21 12:43:26'),
(23, 'Лучко Святослав', '+380977112665', 'svatoslavlucko@gmail.com', '', '2025-11-12 00:05:18'),
(24, 'Іван Петренко', '+380671234567', 'ivan.petrenko@example.com', '', '2025-11-12 00:05:18'),
(25, 'Марія Коваль', '+380933456789', 'maria.koval@example.com', '', '2025-11-12 00:05:18'),
(26, 'Сергій Левченко', '+380503334455', 'sergiy.lev@example.com', '', '2025-11-12 00:05:18'),
(27, 'Оксана Мельник', '+380973112233', 'oksana.melnyk@example.com', '', '2025-11-12 00:05:18'),
(28, 'Віктор Андрущенко', '+380632223344', 'victor.andr@example.com', '', '2025-11-12 00:05:18'),
(29, 'Катерина Бондар', '+380991112233', 'katya.bondar@example.com', '', '2025-11-12 00:05:18'),
(30, 'Дмитро Шевченко', '+380668877665', 'd.shevchenko@example.com', '', '2025-11-12 00:05:18'),
(31, 'Олена Романюк', '+380955667788', 'olena.rom@example.com', '', '2025-11-12 00:05:18'),
(32, 'Назар Білий', '+380984445566', 'n.bilyi@example.com', '', '2025-11-12 00:05:18'),
(33, 'Ірина Кравчук', '+380972223344', 'iryna.kravchuk@example.com', '', '2025-11-12 00:05:18'),
(34, 'Петро Савчук', '+380638889900', 'p.savchuk@example.com', '', '2025-11-12 00:05:18'),
(35, 'Петро Замлин', '', '', '', '2025-12-25 10:26:13'),
(36, 'Андрій Гертруда', '', '', '', '2025-12-25 10:38:35'),
(37, '12334', '', '', '', '2025-12-29 08:07:11');

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
(4, 24, 'Ноутбук', 'Asus TUF FX506', 'Не вмикається', 'new', '—', 2500.000000000000000000000000000000, '2025-11-11 23:46:01.000000'),
(5, 25, 'Телефон', 'Samsung A53', 'Заміна екрана', 'progress', 'Екран A53', 3800.000000000000000000000000000000, '2025-11-11 23:46:01.000000'),
(6, 26, 'ПК', 'Intel i5 + GTX1660', 'Не запускається Windows', 'done', 'SSD 512GB', 2200.000000000000000000000000000000, '2025-11-11 23:46:01.000000'),
(7, 27, 'Ноутбук', 'HP Pavilion 15', 'Заміна клавіатури', 'issued', 'Клавіатура HP15', 1700.000000000000000000000000000000, '2025-11-11 23:46:01.000000'),
(8, 28, 'Телефон', 'iPhone 12', 'Проблеми з зарядкою', 'canceled', 'Порт Type-C', 0.000000000000000000000000000000, '2025-11-11 23:46:01.000000'),
(9, 29, 'Ноутбук', 'Lenovo IdeaPad 3', 'Не працює клавіатура', 'new', 'Клавіатура IdeaPad', 1900.000000000000000000000000000000, '2025-11-11 23:46:02.000000'),
(10, 23, 'ПК', '', 'Не вмикається', 'new', '', 200.000000000000000000000000000000, '2025-11-10 22:00:00.000000'),
(11, 24, 'Ноутбук', 'Dell Inspiron 5510', 'Шумить кулер', 'progress', 'Кулер Dell 5510', 800.000000000000000000000000000000, '2025-11-11 23:49:55.000000'),
(12, 25, 'Телефон', 'iPhone X', 'Заміна акумулятора', 'done', 'Battery iPhone X', 1600.000000000000000000000000000000, '2025-11-11 23:49:55.000000'),
(13, 26, 'ПК', 'MSI MAG B550', 'Не працює LAN порт', 'issued', 'Модуль LAN', 900.000000000000000000000000000000, '2025-11-11 23:49:55.000000'),
(14, 27, 'Ноутбук', 'Acer Aspire 5', 'Не заряджається', 'new', 'Зарядний порт', 1200.000000000000000000000000000000, '2025-11-11 23:49:55.000000'),
(15, 28, 'Телефон', 'Xiaomi Redmi Note 10', 'Розбите скло камери', 'progress', 'Скло камери', 700.000000000000000000000000000000, '2025-11-11 23:49:55.000000'),
(16, 30, 'ПК', 'HP Omen 25L', 'Не вмикається після грози', 'new', 'Блок живлення', 2500.000000000000000000000000000000, '2025-11-11 23:49:55.000000'),
(17, 31, 'Ноутбук', 'Lenovo ThinkPad E14', 'Проблеми з Wi-Fi', 'done', 'Wi-Fi модуль', 600.000000000000000000000000000000, '2025-11-11 23:49:55.000000'),
(18, 32, 'Телефон', 'Samsung S22', 'Не працює сенсор', 'canceled', 'Дисплей S22', 0.000000000000000000000000000000, '2025-11-11 23:49:55.000000'),
(19, 33, 'Планшет', 'Huawei MediaPad M5', 'Розбите скло', 'progress', 'Скло планшета', 950.000000000000000000000000000000, '2025-11-11 23:49:55.000000'),
(20, 34, 'Ноутбук', 'Apple MacBook Air M1', 'Проблема з клавіатурою', 'issued', 'Клавіатура оригінал', 4300.000000000000000000000000000000, '2025-11-11 23:49:55.000000'),
(21, 2, 'ПК', '', 'Замінити SSD', 'new', '', 2300.000000000000000000000000000000, '2025-12-24 22:00:00.000000');

-- --------------------------------------------------------

--
-- Структура таблиці `sale_headers`
--

CREATE TABLE `sale_headers` (
  `Id` int(11) NOT NULL,
  `ClientId` int(11) NOT NULL,
  `ServiceId` int(11) DEFAULT NULL,
  `Price` decimal(65,30) NOT NULL,
  `Date` datetime(6) NOT NULL,
  `Payment` varchar(32) NOT NULL DEFAULT 'Готівка',
  `Status` varchar(32) NOT NULL DEFAULT 'В обробці',
  `Note` longtext DEFAULT NULL,
  `Total` decimal(12,2) NOT NULL DEFAULT 0.00
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Дамп даних таблиці `sale_headers`
--

INSERT INTO `sale_headers` (`Id`, `ClientId`, `ServiceId`, `Price`, `Date`, `Payment`, `Status`, `Note`, `Total`) VALUES
(1, 1, NULL, 0.000000000000000000000000000000, '2025-10-22 10:00:00.000000', 'Готівка', 'done', NULL, 24000.00),
(2, 11, NULL, 0.000000000000000000000000000000, '2025-10-21 12:30:00.000000', 'Картка', 'done', NULL, 32000.00),
(4, 1, NULL, 0.000000000000000000000000000000, '2025-10-25 10:00:00.000000', 'Готівка', 'done', 'Оплачено повністю', 24500.00),
(5, 2, NULL, 0.000000000000000000000000000000, '2025-10-26 12:45:00.000000', 'Картка', 'done', '', 32000.00),
(6, 3, NULL, 0.000000000000000000000000000000, '2025-10-27 14:30:00.000000', 'Готівка', 'processing', 'Очікує підтвердження', 8500.00),
(7, 4, NULL, 0.000000000000000000000000000000, '2025-10-28 09:00:00.000000', 'Картка', 'done', '', 17999.00),
(8, 5, NULL, 0.000000000000000000000000000000, '2025-10-29 11:15:00.000000', 'Готівка', 'cancelled', 'Скасовано покупцем', 12000.00),
(9, 6, NULL, 0.000000000000000000000000000000, '2025-10-30 16:40:00.000000', 'Картка', 'done', '', 2100.00),
(10, 7, NULL, 0.000000000000000000000000000000, '2025-10-31 13:20:00.000000', 'Готівка', 'done', '', 3200.00),
(11, 8, NULL, 0.000000000000000000000000000000, '2025-11-01 15:30:00.000000', 'Картка', 'processing', '', 9000.00),
(12, 9, NULL, 0.000000000000000000000000000000, '2025-11-02 09:45:00.000000', 'Готівка', 'done', '', 6000.00),
(13, 10, NULL, 0.000000000000000000000000000000, '2025-11-03 17:10:00.000000', 'Картка', 'done', 'Знижка 5%', 9500.00),
(15, 23, NULL, 0.000000000000000000000000000000, '2025-11-11 03:00:00.000000', 'Готівка', 'done', '', 50.00),
(16, 35, NULL, 0.000000000000000000000000000000, '2025-12-24 22:00:00.000000', 'Готівка', 'done', '', 12000.00),
(17, 36, NULL, 0.000000000000000000000000000000, '2025-12-24 22:00:00.000000', 'Готівка', 'done', '', 23000.00),
(19, 37, NULL, 0.000000000000000000000000000000, '2025-12-28 22:00:00.000000', 'Готівка', 'done', '', 14434.00),
(20, 21, NULL, 0.000000000000000000000000000000, '2026-03-20 22:00:00.000000', 'Готівка', 'done', '', 7000.00);

-- --------------------------------------------------------

--
-- Структура таблиці `sale_items`
--

CREATE TABLE `sale_items` (
  `Id` int(11) NOT NULL,
  `SaleId` int(11) NOT NULL,
  `Name` longtext NOT NULL,
  `Qty` int(11) NOT NULL DEFAULT 1,
  `Price` decimal(12,2) NOT NULL DEFAULT 0.00
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Дамп даних таблиці `sale_items`
--

INSERT INTO `sale_items` (`Id`, `SaleId`, `Name`, `Qty`, `Price`) VALUES
(1, 1, 'Мишка', 1, 950.00),
(2, 2, 'Монітор', 2, 16000.00),
(4, 4, 'Ноутбук Lenovo IdeaPad 5', 1, 24500.00),
(5, 5, 'Монітор Samsung 27\"', 2, 16000.00),
(6, 6, 'Принтер HP LaserJet M111a', 1, 8500.00),
(7, 7, 'Смартфон Samsung A55', 1, 17999.00),
(8, 8, 'Телевізор LG 43\"', 1, 12000.00),
(9, 9, 'Мишка Logitech M720', 3, 700.00),
(10, 10, 'Клавіатура Keychron K6', 2, 1600.00),
(11, 11, 'Навушники Sony WH-CH720', 2, 4500.00),
(12, 12, 'Бездротова колонка JBL Flip 6', 2, 3000.00),
(13, 13, 'Павербанк Xiaomi 20000mAh', 5, 1900.00),
(15, 15, 'Навушники', 1, 50.00),
(16, 16, 'Ноутбук Dell', 1, 12000.00),
(17, 17, 'ПК', 1, 23000.00),
(19, 19, 'пк', 1, 14434.00),
(20, 20, 'ПК', 1, 7000.00);

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

--
-- Дамп даних таблиці `services`
--

INSERT INTO `services` (`Id`, `Name`, `Description`, `Price`, `Category`) VALUES
(1, 'Клавіатура Hator', '', 2600.000000000000000000000000000000, 'Repair'),
(2, 'Навушники', '', 50.000000000000000000000000000000, 'Repair'),
(3, 'Ноутбук Dell', '', 12000.000000000000000000000000000000, 'Repair'),
(4, 'ПК', '', 23000.000000000000000000000000000000, 'Repair');

-- --------------------------------------------------------

--
-- Структура таблиці `users`
--

CREATE TABLE `users` (
  `Id` int(11) NOT NULL,
  `Username` longtext NOT NULL,
  `PasswordHash` longtext NOT NULL,
  `Email` longtext NOT NULL,
  `Role` longtext NOT NULL,
  `reset_code` varchar(10) DEFAULT NULL,
  `reset_code_expiry` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Дамп даних таблиці `users`
--

INSERT INTO `users` (`Id`, `Username`, `PasswordHash`, `Email`, `Role`, `reset_code`, `reset_code_expiry`) VALUES
(1, 'Admin', '$2a$12$L8ywdLO8/PXfAPIpSQ37KuGF4LO2NQM.Lol4HC4eypzu1v9ZqER3q', 'pashkovskanastasia@gmail.com', 'Admin', NULL, NULL);

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
  ADD PRIMARY KEY (`Id`),
  ADD KEY `fk_repairs_client` (`ClientId`);

--
-- Індекси таблиці `sale_headers`
--
ALTER TABLE `sale_headers`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `fk_sale_headers_client` (`ClientId`),
  ADD KEY `fk_sale_headers_service` (`ServiceId`);

--
-- Індекси таблиці `sale_items`
--
ALTER TABLE `sale_items`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `fk_items_sale` (`SaleId`);

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
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=38;

--
-- AUTO_INCREMENT для таблиці `repairs`
--
ALTER TABLE `repairs`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=23;

--
-- AUTO_INCREMENT для таблиці `sale_headers`
--
ALTER TABLE `sale_headers`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=21;

--
-- AUTO_INCREMENT для таблиці `sale_items`
--
ALTER TABLE `sale_items`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=21;

--
-- AUTO_INCREMENT для таблиці `services`
--
ALTER TABLE `services`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT для таблиці `users`
--
ALTER TABLE `users`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- Обмеження зовнішнього ключа збережених таблиць
--

--
-- Обмеження зовнішнього ключа таблиці `repairs`
--
ALTER TABLE `repairs`
  ADD CONSTRAINT `fk_repairs_client` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON UPDATE CASCADE;

--
-- Обмеження зовнішнього ключа таблиці `sale_headers`
--
ALTER TABLE `sale_headers`
  ADD CONSTRAINT `fk_sale_headers_client` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_sale_headers_service` FOREIGN KEY (`ServiceId`) REFERENCES `services` (`Id`) ON UPDATE CASCADE;

--
-- Обмеження зовнішнього ключа таблиці `sale_items`
--
ALTER TABLE `sale_items`
  ADD CONSTRAINT `fk_items_sale` FOREIGN KEY (`SaleId`) REFERENCES `sale_headers` (`Id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_sale_items_sale` FOREIGN KEY (`SaleId`) REFERENCES `sale_headers` (`Id`) ON DELETE CASCADE ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
