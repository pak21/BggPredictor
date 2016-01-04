library(caret)
library(dplyr)
library(RODBC)

modelForUser <- function (userId, formula) {
  data <- items %>% filter(User_Id == userId) %>% inner_join(games, by = c("Game_Id" = "Id"))
  train(formula, data = data, method = "lm", trControl = control)
}

meanSquaredErrorsForFormula <- function (formula, userIds) {
  sapply(
    userIds,
    function (userId) {
      modelForUser(userId, formula)$results$RMSE
    }
  )
}

convertTimeToMinutes <- function(time) {
  sapply(
    strsplit(as.character(time), ":", fixed = TRUE),
    function (componentStrings) {
      components <- as.numeric(componentStrings)
      components[1] * 60 + components[2] + components[3] / 60
    }
  )
}

db <- odbcDriverConnect(connection="Driver={SQL Server};server=.\\SQLEXPRESS;database=BggPredictor;trusted_connection=True")
users <- sqlFetch(db, "Users") %>%
  filter(Username != "joewyatt7")  # Joseph hasn't rated any games...
games <- sqlFetch(db, "Games") %>% rename(BggRating = Rating) %>%
  mutate(PlayingTime = convertTimeToMinutes(PlayingTime)) %>%
  mutate(ConstrainedMaximumPlayers = pmin(MaximumPlayers, 8))
items <- sqlFetch(db, "CollectionItems")
odbcClose(db)

control <- trainControl(method = "repeatedcv", number = 5, repeats = 1)

formulae <- c(
  Rating ~ BggRating,
  Rating ~ BggRating + Weight,
  Rating ~ BggRating + Weight + PlayingTime,
  Rating ~ BggRating + BayesRating + Weight + MinimumPlayers + MaximumPlayers + PlayingTime + MinimumAge,
  Rating ~ BggRating + BayesRating + Weight + MinimumPlayers + ConstrainedMaximumPlayers + PlayingTime + MinimumAge
)

set.seed(4669)
errors <- sapply(
  formulae,
  function (formula) {
    mean(meanSquaredErrorsForFormula(formula, users$Id))
  }
)

#predictions <- games %>% mutate(Prediction = predict(model, games)) %>% filter(!(Id %in% data$Game_Id))

