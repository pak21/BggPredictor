library(DAAG)
library(dplyr)
library(RODBC)

meanSquaredErrorForUser <- function (userId, formula) {
  data <- items %>% filter(User_Id == userId) %>% inner_join(games, by = c("Game_Id" = "Id"))
  model <- lm(formula, data = data)
  crossvalidation <- cv.lm(data = data, form.lm = model, m = 5, plotit = FALSE)
  attr(crossvalidation, "ms")
}

meanSquaredErrorsForFormula <- function (formula, userIds) {
  as.numeric(Map(function (userId) { meanSquaredErrorForUser(userId, formula) }, userIds))
}

meanMeanSquaredErrorForFormula <- function (formula) {
  mean((users %>% mutate(MeanSquaredError = meanSquaredErrorsForFormula(formula, Id)))$MeanSquaredError)
}

db <- odbcDriverConnect(connection="Driver={SQL Server};server=.\\SQLEXPRESS;database=BggPredictor;trusted_connection=True")
users <- sqlFetch(db, "Users") %>%
  filter(Username != "joewyatt7")  # Joseph hasn't rated any games...
games <- sqlFetch(db, "Games") %>% rename(BggRating = Rating) %>%
  mutate(ConstrainedMaximumPlayers = pmin(MaximumPlayers, 8))
items <- sqlFetch(db, "CollectionItems")
odbcClose(db)

baseFormula <- Rating ~ BggRating + BayesRating + Weight + MinimumPlayers + MaximumPlayers + MinimumAge

set.seed(4669)

data <- items %>% filter(User_Id == 1) %>% inner_join(games, by = c("Game_Id" = "Id"))

control <- trainControl(method = "repeatedcv", number = 5, repeats = 1)
caretFit <- train(baseFormula, data = data, method = "lm", trControl = control)

model <- lm(baseFormula, data = data)
crossvalidation <- cv.lm(data = data, form.lm = model, m = 5, plotit = FALSE)



