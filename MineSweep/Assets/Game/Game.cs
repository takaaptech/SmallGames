using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

namespace DefaultNamespace {
    public class Game {
        public int row;
        public int col;
        public float rate;
        public bool isGameOver;
        public int timer;
        public bool isFlagMode = false;

        public enum EGameStatus {
            InGame,
            Falied,
            Succ,
        }

        private EGameStatus gameState;
        private float startTimer;

        private Dictionary<Vector2Int, Cell> allCells = new Dictionary<Vector2Int, Cell>();
        public Action<Vector2Int, int> OnCellStateChanged;

        public void DoUpdate(){
            // update timer
            if (gameState == EGameStatus.InGame) {
                var duration = Time.realtimeSinceStartup - startTimer;
                timer = (int) duration;
            }
        }


        public void OnCreateCell(Cell cell, Vector2Int pos){
            if (cell == null) return;
            cell.pos = pos;
            cell.btn.onClick.AddListener(() => { OnClickCell(pos); });
            allCells.Add(pos, cell);
        }

        public void OnFinishCreateCells(){
            Profiler.BeginSample("InitCellStates");
            InitCellStates();
            Profiler.EndSample();
        }

        public int[,] cellStatus;
        public const int MINE_TAG = 9; //每个位置不可能超过8个的雷
        public const int DIRTY_VAL = MINE_TAG + 1;
        public const int FLAG_VAL = 100;

        public int TotalCellCount = 0;
        public int TotalMineCount = 0;
        private int curFlagCount = 0;
        private int curOpenedCount = 0;


        public bool IsEmptyCell(int val){
            return val == 0;
        }

        public bool IsMine(int val){
            return val >= MINE_TAG;
        }

        public bool IsDirty(int val){
            return val >= DIRTY_VAL;
        }

        public int GetDirtyVal(int val){
            if (val >= DIRTY_VAL) {
                return val;
            }

            return DIRTY_VAL + val;
        }

        public int GetRawVal(int val){
            if (val >= FLAG_VAL) {
                return val - FLAG_VAL;
            }
            else if (val >= DIRTY_VAL) {
                return val - DIRTY_VAL;
            }
            else {
                return val;
            }
        }

        public void MarkDirty(Vector2Int pos){
            var val = cellStatus[pos.x, pos.y];
            if (!IsDirty(val)) {
                cellStatus[pos.x, pos.y] = GetDirtyVal(val);
            }
        }

        public void MarkUnDirty(Vector2Int pos){
            var val = cellStatus[pos.x, pos.y];
            if (IsDirty(val)) {
                cellStatus[pos.x, pos.y] = GetRawVal(val);
            }
        }

        void InitCellStates(){
            if (Global.config.isDebug) {
                Random.InitState(1000);
            }

            TotalCellCount = row * col;
            var mineCount = (int) (TotalCellCount * rate);
            cellStatus = new int[row, col];
            for (int _row = 0; _row < row; _row++) {
                for (int _col = 0; _col < col; _col++) {
                    cellStatus[_row, _col] = Random.value < rate ? MINE_TAG : 0;
                }
            }

            for (int _row = 0; _row < row; _row++) {
                for (int _col = 0; _col < col; _col++) {
                    if (cellStatus[_row, _col] != MINE_TAG) {
                        cellStatus[_row, _col] = GetMineCount(cellStatus, _row, _col);
                    }
                }
            }

            TotalMineCount = 0;
            for (int _row = 0; _row < row; _row++) {
                for (int _col = 0; _col < col; _col++) {
                    if (cellStatus[_row, _col] == MINE_TAG) {
                        ++TotalMineCount;
                    }
                }
            }

            curFlagCount = 0;
            curOpenedCount = 0;
            if (Global.config.isDebug) {
                for (int _row = 0; _row < row; _row++) {
                    for (int _col = 0; _col < col; _col++) {
                        var val = cellStatus[_row, _col];
                        var cell = allCells[new Vector2Int(_row, _col)];
                        if (val == MINE_TAG) {
                            cell.SetState(-1);
                        }
                        else {
                            cell.SetState(val);
                        }
                    }
                }
            }
        }

        int GetMineCount(int[,] ary, int r, int c){
            int sum = 0;
            for (int _row = r - 1; _row <= r + 1; _row++) {
                for (int _col = c - 1; _col <= c + 1; _col++) {
                    if (_row < 0 || _row >= row || _col < 0 || _col >= col) {
                        continue;
                    }

                    if (ary[_row, _col] == MINE_TAG) {
                        ++sum;
                    }
                }
            }

            return sum;
        }

        public void OnClickCell(Vector2Int pos){
            Debug.Log("ClickButton " + pos + " isFlagMode" + isFlagMode);
            if (isFlagMode) {
                TryFlagCell(pos);
            }
            else {
                TryOpenCell(pos);
            }
        }

        private Queue<Vector2Int> spaceQueue = new Queue<Vector2Int>();

        public void TryOpenCell(Vector2Int pos){
            var val = cellStatus[pos.x, pos.y];
            if (IsDirty(val)) {
                return;
            }

            if (IsMine(val)) {
                OnFalied();
            }

            spaceQueue.Clear();
            spaceQueue.Enqueue(pos);
            while (spaceQueue.Count > 0) {
                var nextPos = spaceQueue.Dequeue();
                var r = nextPos.x;
                var c = nextPos.y;
                var nextVal = cellStatus[r, c];
                var cell = allCells[nextPos];
                if (!IsDirty(nextVal) && !IsMine(nextVal)) {
                    ++curOpenedCount;
                    cell.SetState(nextVal);
                    MarkDirty(nextPos);
                }

                if (IsEmptyCell(nextVal)) {
                    //check border cell
                    for (int _row = r - 1; _row <= r + 1; _row++) {
                        for (int _col = c - 1; _col <= c + 1; _col++) {
                            if (_row < 0 || _row >= row || _col < 0 || _col >= col) {
                                continue;
                            }

                            spaceQueue.Enqueue(new Vector2Int(_row, _col));
                        }
                    }
                }
            }

            if (curOpenedCount + curFlagCount >= TotalCellCount) {
                OnSucc();
            }
        }

        public void TryFlagCell(Vector2Int pos){
            var val = cellStatus[pos.x, pos.y];
            if (val >= FLAG_VAL) {
                cellStatus[pos.x, pos.y] = val - FLAG_VAL;
                var cell = allCells[pos];
                cell.Reset();
                --curFlagCount;
            }
            else {
                if (curFlagCount == TotalMineCount) {
                    Debug.LogError("you has flag to much! ");
                }

                if (!IsDirty(val)) {
                    cellStatus[pos.x, pos.y] = val + FLAG_VAL;
                    var cell = allCells[pos];
                    cell.SetState(ECellType.Flag);
                    ++curFlagCount;
                }
                else {
                    Debug.Log("Try to flag a dirty cell" + pos);
                }
            }

            if (curOpenedCount + curFlagCount >= TotalCellCount) {
                OnSucc();
            }
        }

        void OnFalied(){
            ShowResult("Boom!!! Falied");
        }

        void OnSucc(){
            //upload user timer score
            UploadUserScore(Global.Player.userID, Global.GameType, Global.SmallGameType, timer);
            ShowResult("You win!");
        }

        void ShowResult(string info){
            Debug.LogError(info);
        }

        // 上传到排行榜
        void UploadUserScore(long userID, int gameType, int smallGameType, int value){
            Debug.LogError("Upload Game Result userID = {0} gmaeType = {1} smallGameType = {2} val = {3}");
        }
    }
}