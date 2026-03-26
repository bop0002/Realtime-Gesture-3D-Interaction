import os
import numpy as np
import pandas as pd
import tensorflow as tf
from sklearn.model_selection import train_test_split
from sklearn.metrics import confusion_matrix, classification_report, accuracy_score



# 1. Đọc dữ liệu
dataset_path = "models/gesture_classification/gesture_classification.csv"
dataset = pd.read_csv(dataset_path, header=None)

X = dataset.iloc[:, 1:].values
y = dataset.iloc[:, 0].values

print("Dataset shape:", dataset.shape)

if X.shape[1] != 42:
    raise ValueError("Feature phải là 42 (21 landmark x,y)")

# 2. Chia train / test
X_train, X_test, y_train, y_test = train_test_split(
    X,
    y,
    train_size=0.75,
    stratify=y,
    random_state=42
)

NUM_CLASSES = len(np.unique(y))
print("Number of classes:", NUM_CLASSES)

# 3. Xây dựng model
model = tf.keras.models.Sequential([
    tf.keras.layers.Input(shape=(42,)),
    tf.keras.layers.Dense(64, activation='relu'),
    tf.keras.layers.Dropout(0.3),
    tf.keras.layers.Dense(32, activation='relu'),
    tf.keras.layers.Dense(NUM_CLASSES, activation='softmax')
])

model.compile(
    optimizer=tf.keras.optimizers.Adam(learning_rate=0.001),
    loss='sparse_categorical_crossentropy',
    metrics=['accuracy']
)

model.summary()

# 4. Callback
early_stopping = tf.keras.callbacks.EarlyStopping(
    monitor='val_loss',
    patience=20,
    restore_best_weights=True
)

# 5. Train model
history = model.fit(
    X_train,
    y_train,
    epochs=500,
    batch_size=32,
    validation_split=0.2,
    callbacks=[early_stopping],
    verbose=1
)

# 6. Lưu log ra CSV
os.makedirs("models/gesture_classification", exist_ok=True)

history_df = pd.DataFrame(history.history)
csv_path = "models/gesture_classification/train_log.csv"
history_df.to_csv(csv_path, index=False)

print(f"Saved training log to: {csv_path}")

# 7. Đánh giá model
score = model.evaluate(X_test, y_test, verbose=1)

print(f"\nTest loss: {score[0]:.4f}")
print(f"Test accuracy: {score[1]*100:.2f}%")

y_pred_prob = model.predict(X_test, verbose=1)
y_pred = np.argmax(y_pred_prob, axis=1)

acc = accuracy_score(y_test, y_pred)
print(f"Accuracy (sklearn): {acc*100:.2f}%")

print("\nConfusion Matrix:")
print(confusion_matrix(y_test, y_pred))

print("\nClassification Report:")
print(classification_report(y_test, y_pred, digits=4))

# 9. Lưu model
model_path = "models/gesture_classification/gesture_classification_model.keras"
model.save(model_path)

print(f"Saved model to: {model_path}")

# 10. Convert sang TFLite
converter = tf.lite.TFLiteConverter.from_keras_model(model)
tflite_model = converter.convert()

tflite_path = "models/gesture_classification/gesture_classification.tflite"
with open(tflite_path, "wb") as f:
    f.write(tflite_model)

print(f"Saved TFLite model to: {tflite_path}")