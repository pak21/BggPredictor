library(caret)
library(dplyr)
library(neuralnet)
library(RODBC)

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
games <- sqlFetch(db, "Games") %>%
  mutate(
    Rating = Rating / 10,
    BayesRating = BayesRating / 10,
    Weight = Weight / 5,
    MinimumPlayers = MinimumPlayers / 8,
    MaximumPlayers = pmin(MaximumPlayers, 8) / 8,
    PlayingTime = convertTimeToMinutes(PlayingTime) / 180,
    MinimumAge = MinimumAge / 21
  )
items <- sqlFetch(db, "CollectionItems") %>% rename(UserRating = Rating) %>%
  mutate(UserRating = UserRating / 10)
odbcClose(db)

data <- items %>% filter(User_Id == 3) %>% inner_join(games, by = c("Game_Id" = "Id"))
formula <- UserRating ~ Rating + BayesRating + Weight + MinimumPlayers + MaximumPlayers + PlayingTime + MinimumAge

control <- trainControl(method = "repeatedcv", number = 5, repeats = 1)

nnfit <- train(
  formula,
  data = data,
  method = "neuralnet",
  trControl = control,
  tuneGrid = data.frame(layer1 = 2, layer2 = 0, layer3 = 0)
)

lmfit <- train(
  formula,
  data = data,
  method = "lm",
  trControl = control
)

simplelmfit <- train(
  UserRating ~ Rating,
  data = data,
  method = "lm",
  trControl = control
)

errors <- c(simplelmfit$results$RMSE, lmfit$results$RMSE, nnfit$results$RMSE)