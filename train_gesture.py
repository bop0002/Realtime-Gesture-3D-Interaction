import numpy as np
import pandas as pd
import tensorflow as tf
from sklearn.model_selection import train_test_split
from sklearn.metrics import confusion_matrix, classification_report

dataset_path = "models/gesture_classification/gesture_classification.csv"

dataset = pd.read_csv(dataset_path, header=None)

X = dataset.iloc[:, 1:].values
y = dataset.iloc[:, 0].values

print("Dataset shape:", dataset.shape)

if X.shape[1] != 42:
    raise ValueError("Feature phải là 42 (21 landmark x,y)")

X_train, X_test, y_train, y_test = train_test_split(
    X,
    y,
    train_size=0.75,
    stratify=y,
    random_state=42
)

NUM_CLASSES = len(np.unique(y))
#MLP
model = tf.keras.models.Sequential([
    tf.keras.layers.Input((42,)),
    tf.keras.layers.Dense(64, activation='relu'),
    tf.keras.layers.Dropout(0.3),
    tf.keras.layers.Dense(32, activation='relu'),
    tf.keras.layers.Dense(NUM_CLASSES, activation='softmax')
])

model.compile(
    optimizer='adam',
    loss='sparse_categorical_crossentropy',
    metrics=['accuracy']
)

model.summary()

callback = tf.keras.callbacks.EarlyStopping(
    patience=20,
    restore_best_weights=True
)

model.fit(
    X_train,
    y_train,
    epochs=500,
    batch_size=32,
    validation_split=0.2,
    callbacks=[callback]
)

score = model.evaluate(X_test, y_test)

print("Test accuracy:", score[1])

y_pred = np.argmax(model.predict(X_test), axis=1)

print("\nConfusion Matrix:")
print(confusion_matrix(y_test, y_pred))

print("\nClassification Report:")
print(classification_report(y_test, y_pred))

converter = tf.lite.TFLiteConverter.from_keras_model(model)
tflite_model = converter.convert()

with open(
    "models/gesture_classification/gesture_classification.tflite",
    "wb"
) as f:
    f.write(tflite_model)

print("Saved TFLite model")